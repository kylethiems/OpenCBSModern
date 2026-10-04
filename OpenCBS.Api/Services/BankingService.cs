using Microsoft.EntityFrameworkCore;
using OpenCBS.Api.Data;
using OpenCBS.Core.Domain.Accounting;
using OpenCBS.Core.Domain.Clients;
using OpenCBS.Core.Domain.Loans;
using OpenCBS.Core.Domain.Savings;
using OpenCBS.Core.Engine;
using OpenCBS.Core.Exceptions;

namespace OpenCBS.Api.Services;

public class BankingService : IBankingService
{
    private readonly OpenCbsDbContext _db;

    public BankingService(OpenCbsDbContext db)
    {
        _db = db;
    }

    // ================= CLIENT MANAGEMENT =================

    public async Task<Client> CreateClientAsync(CreateClientDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.IdentificationNumber))
            throw new BankingException("Identification number is required.");

        var client = new Client
        {
            IdentificationNumber = dto.IdentificationNumber.Trim(),
            FirstName = dto.FirstName?.Trim() ?? string.Empty,
            LastName = dto.LastName?.Trim() ?? string.Empty,
            CompanyName = dto.CompanyName?.Trim() ?? string.Empty,
            ClientType = dto.ClientType,
            Status = ClientStatus.PendingKyc,
            Email = dto.Email?.Trim() ?? string.Empty,
            PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty,
            Address = dto.Address?.Trim() ?? string.Empty,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Clients.Add(client);
        await _db.SaveChangesAsync();
        return client;
    }

    public async Task<Client?> GetClientByIdAsync(int id)
    {
        return await _db.Clients.FindAsync(id);
    }

    public async Task<List<Client>> GetAllClientsAsync()
    {
        return await _db.Clients.AsNoTracking().ToListAsync();
    }

    public async Task<Client> UpdateKycStatusAsync(int clientId, ClientStatus status)
    {
        var client = await _db.Clients.FindAsync(clientId)
            ?? throw new BankingException($"Client with ID {clientId} not found.");

        client.Status = status;
        await _db.SaveChangesAsync();
        return client;
    }

    // ================= LOAN PORTFOLIO MANAGEMENT =================

    public async Task<LoanContract> ApplyForLoanAsync(ApplyLoanDto dto)
    {
        var client = await _db.Clients.FindAsync(dto.ClientId)
            ?? throw new BankingException($"Client with ID {dto.ClientId} not found.");

        if (client.Status != ClientStatus.Active)
            throw new BankingException($"Client must be in Active KYC status to apply for loans. Current status: {client.Status}");

        var contractCode = $"LN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var loan = new LoanContract
        {
            ContractCode = contractCode,
            ClientId = client.Id,
            PrincipalAmount = dto.PrincipalAmount,
            AnnualInterestRate = dto.AnnualInterestRate,
            InstallmentCount = dto.InstallmentCount,
            GracePeriodMonths = dto.GracePeriodMonths,
            CalculationMethod = dto.CalculationMethod,
            Status = LoanStatus.PendingApproval,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Generate preliminary amortization schedule
        var schedule = ScheduleBuilder.BuildSchedule(
            dto.PrincipalAmount,
            dto.AnnualInterestRate,
            dto.InstallmentCount,
            DateTime.UtcNow,
            dto.CalculationMethod,
            dto.GracePeriodMonths);

        loan.Installments = schedule;

        _db.LoanContracts.Add(loan);
        await _db.SaveChangesAsync();
        return loan;
    }

    public async Task<LoanContract?> GetLoanByIdAsync(int id)
    {
        return await _db.LoanContracts
            .Include(l => l.Client)
            .Include(l => l.Installments)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<List<LoanContract>> GetLoansByClientAsync(int clientId)
    {
        return await _db.LoanContracts
            .Include(l => l.Installments)
            .Where(l => l.ClientId == clientId)
            .ToListAsync();
    }

    public async Task<LoanContract> ApproveLoanAsync(int loanId)
    {
        var loan = await _db.LoanContracts.FindAsync(loanId)
            ?? throw new BankingException($"Loan with ID {loanId} not found.");

        if (loan.Status != LoanStatus.PendingApproval)
            throw new InvalidContractStateException($"Only pending loans can be approved. Current status: {loan.Status}");

        loan.Status = LoanStatus.Approved;
        await _db.SaveChangesAsync();
        return loan;
    }

    public async Task<LoanContract> DisburseLoanAsync(int loanId)
    {
        var loan = await _db.LoanContracts
            .Include(l => l.Installments)
            .FirstOrDefaultAsync(l => l.Id == loanId)
            ?? throw new BankingException($"Loan with ID {loanId} not found.");

        if (loan.Status != LoanStatus.Approved)
            throw new InvalidContractStateException($"Only approved loans can be disbursed. Current status: {loan.Status}");

        loan.Status = LoanStatus.Disbursed;
        loan.DisbursementDateUtc = DateTime.UtcNow;

        // Recalculate schedule starting from exact disbursement date
        var schedule = ScheduleBuilder.BuildSchedule(
            loan.PrincipalAmount,
            loan.AnnualInterestRate,
            loan.InstallmentCount,
            loan.DisbursementDateUtc.Value,
            loan.CalculationMethod,
            loan.GracePeriodMonths);

        _db.LoanInstallments.RemoveRange(loan.Installments);
        loan.Installments = schedule;

        // Post General Ledger double-entry booking:
        // Debit: 1200 Loans Portfolio Receivable
        // Credit: 1010 Cash and Vault
        await PostInternalJournalEntryAsync(
            $"Disbursement of Loan {loan.ContractCode}",
            new List<(string Account, decimal Debit, decimal Credit)>
            {
                ("1200", loan.PrincipalAmount, 0m),
                ("1010", 0m, loan.PrincipalAmount)
            });

        await _db.SaveChangesAsync();
        return loan;
    }

    public async Task<RepaymentResult> ProcessRepaymentAsync(int loanId, decimal paymentAmount)
    {
        if (paymentAmount <= 0)
            throw new BankingException("Repayment amount must be positive.");

        var loan = await _db.LoanContracts
            .Include(l => l.Installments)
            .FirstOrDefaultAsync(l => l.Id == loanId)
            ?? throw new BankingException($"Loan with ID {loanId} not found.");

        if (loan.Status != LoanStatus.Disbursed)
            throw new InvalidContractStateException($"Loan must be disbursed to receive repayments. Current status: {loan.Status}");

        decimal remainingToAllocate = paymentAmount;
        decimal totalPrincipalPaid = 0m;
        decimal totalInterestPaid = 0m;

        var unpaidInstallments = loan.Installments
            .Where(i => !i.IsPaid)
            .OrderBy(i => i.InstallmentNumber)
            .ToList();

        foreach (var inst in unpaidInstallments)
        {
            if (remainingToAllocate <= 0) break;

            // 1. Pay interest first
            decimal interestShortfall = inst.InterestDue - inst.PaidInterest;
            if (interestShortfall > 0)
            {
                decimal interestPayment = Math.Min(remainingToAllocate, interestShortfall);
                inst.PaidInterest += interestPayment;
                totalInterestPaid += interestPayment;
                remainingToAllocate -= interestPayment;
            }

            // 2. Pay principal next
            if (remainingToAllocate > 0)
            {
                decimal principalShortfall = inst.PrincipalDue - inst.PaidPrincipal;
                if (principalShortfall > 0)
                {
                    decimal principalPayment = Math.Min(remainingToAllocate, principalShortfall);
                    inst.PaidPrincipal += principalPayment;
                    totalPrincipalPaid += principalPayment;
                    remainingToAllocate -= principalPayment;
                }
            }

            inst.PaidTotal = inst.PaidPrincipal + inst.PaidInterest;
            if (inst.PaidTotal >= inst.TotalDue)
            {
                inst.IsPaid = true;
                inst.PaidDateUtc = DateTime.UtcNow;
            }
        }

        // Post General Ledger double-entry transaction:
        // Debit: 1010 Cash and Vault (total payment)
        // Credit: 1200 Loans Portfolio Receivable (principal component)
        // Credit: 4100 Interest Income from Loans (interest component)
        decimal actualPaid = totalPrincipalPaid + totalInterestPaid;
        if (actualPaid > 0)
        {
            await PostInternalJournalEntryAsync(
                $"Repayment on Loan {loan.ContractCode}",
                new List<(string Account, decimal Debit, decimal Credit)>
                {
                    ("1010", actualPaid, 0m),
                    ("1200", 0m, totalPrincipalPaid),
                    ("4100", 0m, totalInterestPaid)
                });
        }

        bool allClosed = loan.Installments.All(i => i.IsPaid);
        if (allClosed)
        {
            loan.Status = LoanStatus.Closed;
        }

        await _db.SaveChangesAsync();

        return new RepaymentResult(
            loan.Id,
            actualPaid,
            totalPrincipalPaid,
            totalInterestPaid,
            loan.OutstandingBalance,
            allClosed);
    }

    // ================= SAVINGS & DEPOSIT MANAGEMENT =================

    public async Task<SavingsAccount> OpenSavingsAccountAsync(
        int clientId,
        SavingsAccountType type,
        decimal initialDeposit,
        decimal overdraftLimit = 0m)
    {
        var client = await _db.Clients.FindAsync(clientId)
            ?? throw new BankingException($"Client with ID {clientId} not found.");

        if (client.Status != ClientStatus.Active)
            throw new BankingException($"Client must be in Active KYC status to open accounts.");

        var accountNumber = $"SAV-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var account = new SavingsAccount
        {
            AccountNumber = accountNumber,
            ClientId = client.Id,
            AccountType = type,
            Status = SavingsAccountStatus.Active,
            Balance = 0.00m,
            AnnualInterestRate = type == SavingsAccountType.TermDeposit ? 0.0500m : 0.0200m,
            AllowOverdraft = overdraftLimit > 0,
            OverdraftLimit = overdraftLimit,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.SavingsAccounts.Add(account);
        await _db.SaveChangesAsync();

        if (initialDeposit > 0)
        {
            await DepositAsync(account.Id, initialDeposit, "Initial Account Opening Deposit");
        }

        return account;
    }

    public async Task<SavingsAccount?> GetSavingsAccountByIdAsync(int id)
    {
        return await _db.SavingsAccounts
            .Include(a => a.Client)
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<SavingsAccount>> GetSavingsAccountsByClientAsync(int clientId)
    {
        return await _db.SavingsAccounts
            .Include(a => a.Transactions)
            .Where(a => a.ClientId == clientId)
            .ToListAsync();
    }

    public async Task<SavingsAccount> DepositAsync(int accountId, decimal amount, string description = "Cash Deposit")
    {
        if (amount <= 0)
            throw new BankingException("Deposit amount must be positive.");

        var account = await _db.SavingsAccounts
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.Id == accountId)
            ?? throw new BankingException($"Savings account with ID {accountId} not found.");

        if (account.Status != SavingsAccountStatus.Active)
            throw new AccountFrozenException($"Cannot deposit to account in status: {account.Status}");

        account.Balance += amount;

        var tx = new SavingsTransaction
        {
            SavingsAccountId = account.Id,
            TransactionType = SavingsTransactionType.Deposit,
            Amount = amount,
            BalanceAfter = account.Balance,
            Description = description,
            TimestampUtc = DateTime.UtcNow
        };

        _db.SavingsTransactions.Add(tx);

        // General Ledger:
        // Debit: 1010 Cash and Vault
        // Credit: 2100 Client Savings Deposits
        await PostInternalJournalEntryAsync(
            $"Deposit to {account.AccountNumber}",
            new List<(string Account, decimal Debit, decimal Credit)>
            {
                ("1010", amount, 0m),
                ("2100", 0m, amount)
            });

        await _db.SaveChangesAsync();
        return account;
    }

    public async Task<SavingsAccount> WithdrawAsync(int accountId, decimal amount, string description = "Cash Withdrawal")
    {
        if (amount <= 0)
            throw new BankingException("Withdrawal amount must be positive.");

        var account = await _db.SavingsAccounts
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.Id == accountId)
            ?? throw new BankingException($"Savings account with ID {accountId} not found.");

        if (account.Status != SavingsAccountStatus.Active)
            throw new AccountFrozenException($"Cannot withdraw from account in status: {account.Status}");

        if (amount > account.AvailableFunds)
            throw new InsufficientFundsException($"Insufficient funds. Available: {account.AvailableFunds:C2}, Requested: {amount:C2}");

        account.Balance -= amount;

        var tx = new SavingsTransaction
        {
            SavingsAccountId = account.Id,
            TransactionType = SavingsTransactionType.Withdrawal,
            Amount = amount,
            BalanceAfter = account.Balance,
            Description = description,
            TimestampUtc = DateTime.UtcNow
        };

        _db.SavingsTransactions.Add(tx);

        // General Ledger:
        // Debit: 2100 Client Savings Deposits
        // Credit: 1010 Cash and Vault
        await PostInternalJournalEntryAsync(
            $"Withdrawal from {account.AccountNumber}",
            new List<(string Account, decimal Debit, decimal Credit)>
            {
                ("2100", amount, 0m),
                ("1010", 0m, amount)
            });

        await _db.SaveChangesAsync();
        return account;
    }

    // ================= ACCOUNTING & GENERAL LEDGER =================

    public async Task<JournalEntry> PostJournalEntryAsync(PostJournalEntryDto dto)
    {
        var formattedLines = dto.Lines.Select(l => (l.AccountNumber, l.DebitAmount, l.CreditAmount, l.Memo)).ToList();
        return await PostInternalJournalEntryWithMemoAsync(dto.Description, formattedLines);
    }

    public async Task<List<TrialBalanceItem>> GetTrialBalanceAsync()
    {
        var accounts = await _db.LedgerAccounts.AsNoTracking().ToListAsync();
        return accounts.Select(a => new TrialBalanceItem(a.AccountNumber, a.Name, a.Category, a.Balance)).ToList();
    }

    private async Task<JournalEntry> PostInternalJournalEntryAsync(
        string description,
        List<(string Account, decimal Debit, decimal Credit)> entries)
    {
        var mapped = entries.Select(e => (e.Account, e.Debit, e.Credit, string.Empty)).ToList();
        return await PostInternalJournalEntryWithMemoAsync(description, mapped);
    }

    private async Task<JournalEntry> PostInternalJournalEntryWithMemoAsync(
        string description,
        List<(string Account, decimal Debit, decimal Credit, string Memo)> entries)
    {
        decimal totalDebits = entries.Sum(e => e.Debit);
        decimal totalCredits = entries.Sum(e => e.Credit);

        if (Math.Abs(totalDebits - totalCredits) > 0.0001m)
            throw new UnbalancedJournalException($"Journal entry is unbalanced. Total Debits: {totalDebits:C2}, Total Credits: {totalCredits:C2}");

        var refNumber = $"JE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var journal = new JournalEntry
        {
            ReferenceNumber = refNumber,
            Description = description,
            PostingDateUtc = DateTime.UtcNow
        };

        foreach (var entry in entries)
        {
            var ledgerAcc = await _db.LedgerAccounts.FirstOrDefaultAsync(a => a.AccountNumber == entry.Account)
                ?? throw new BankingException($"Ledger account '{entry.Account}' does not exist in Chart of Accounts.");

            // Update balance according to natural accounting sign
            if (ledgerAcc.Category is AccountCategory.Asset or AccountCategory.Expense)
            {
                ledgerAcc.Balance += (entry.Debit - entry.Credit);
            }
            else
            {
                ledgerAcc.Balance += (entry.Credit - entry.Debit);
            }

            journal.Lines.Add(new JournalEntryLine
            {
                LedgerAccountId = ledgerAcc.Id,
                DebitAmount = entry.Debit,
                CreditAmount = entry.Credit,
                Memo = entry.Memo
            });
        }

        _db.JournalEntries.Add(journal);
        return journal;
    }
}
