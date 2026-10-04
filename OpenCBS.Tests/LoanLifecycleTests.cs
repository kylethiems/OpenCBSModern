using Microsoft.EntityFrameworkCore;
using OpenCBS.Api.Data;
using OpenCBS.Api.Services;
using OpenCBS.Core.Domain.Clients;
using OpenCBS.Core.Domain.Loans;
using OpenCBS.Core.Exceptions;

namespace OpenCBS.Tests;

public class LoanLifecycleTests
{
    private OpenCbsDbContext CreateInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<OpenCbsDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        var db = new OpenCbsDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task LoanApplication_RequiresActiveKycStatus()
    {
        var db = CreateInMemoryDb(nameof(LoanApplication_RequiresActiveKycStatus));
        var service = new BankingService(db);

        // Create client with Pending KYC
        var client = await service.CreateClientAsync(new CreateClientDto(
            "ID-9921", "Maria", "Gonzalez", "", ClientType.Individual, "maria@example.com", "555-0101", "Calle 10"));

        var loanDto = new ApplyLoanDto(
            client.Id, 2_000.00m, 0.12m, 6, 0, InterestCalculationMethod.Annuity);

        // Must reject loan application if KYC is not Active
        await Assert.ThrowsAsync<BankingException>(async () =>
        {
            await service.ApplyForLoanAsync(loanDto);
        });

        // Activate KYC
        await service.UpdateKycStatusAsync(client.Id, ClientStatus.Active);

        // Application now succeeds
        var loan = await service.ApplyForLoanAsync(loanDto);
        Assert.NotNull(loan);
        Assert.Equal(LoanStatus.PendingApproval, loan.Status);
        Assert.Equal(6, loan.Installments.Count);
    }

    [Fact]
    public async Task Disbursement_UpdatesStatusAndPostsLedgerEntry()
    {
        var db = CreateInMemoryDb(nameof(Disbursement_UpdatesStatusAndPostsLedgerEntry));
        var service = new BankingService(db);

        var client = await service.CreateClientAsync(new CreateClientDto(
            "ID-1001", "Ahmed", "Hassan", "", ClientType.Individual, "ahmed@example.com", "555-0202", "Cairo St"));
        await service.UpdateKycStatusAsync(client.Id, ClientStatus.Active);

        var loan = await service.ApplyForLoanAsync(new ApplyLoanDto(
            client.Id, 5_000.00m, 0.10m, 5, 0, InterestCalculationMethod.DecliningBalance));

        // Attempt disbursement before approval must fail
        await Assert.ThrowsAsync<InvalidContractStateException>(async () =>
        {
            await service.DisburseLoanAsync(loan.Id);
        });

        // Approve and disburse
        await service.ApproveLoanAsync(loan.Id);
        var disbursedLoan = await service.DisburseLoanAsync(loan.Id);

        Assert.Equal(LoanStatus.Disbursed, disbursedLoan.Status);
        Assert.NotNull(disbursedLoan.DisbursementDateUtc);

        // Check General Ledger: Loans Receivable (1200) should increase by 5000, Cash (1010) decrease by 5000
        var loansRec = await db.LedgerAccounts.FirstAsync(a => a.AccountNumber == "1200");
        Assert.Equal(5_000.00m, loansRec.Balance);

        var vaultCash = await db.LedgerAccounts.FirstAsync(a => a.AccountNumber == "1010");
        Assert.Equal(1_000_000.00m - 5_000.00m, vaultCash.Balance);
    }

    [Fact]
    public async Task Repayment_AllocatesToInterestThenPrincipal_AndClosesLoanWhenFullyPaid()
    {
        var db = CreateInMemoryDb(nameof(Repayment_AllocatesToInterestThenPrincipal_AndClosesLoanWhenFullyPaid));
        var service = new BankingService(db);

        var client = await service.CreateClientAsync(new CreateClientDto(
            "ID-2002", "Elena", "Rostova", "", ClientType.Individual, "elena@example.com", "555-0303", "Nevsky Ave"));
        await service.UpdateKycStatusAsync(client.Id, ClientStatus.Active);

        var loan = await service.ApplyForLoanAsync(new ApplyLoanDto(
            client.Id, 1_000.00m, 0.12m, 2, 0, InterestCalculationMethod.DecliningBalance));
        await service.ApproveLoanAsync(loan.Id);
        await service.DisburseLoanAsync(loan.Id);

        // Total due on loan
        var freshLoan = await service.GetLoanByIdAsync(loan.Id);
        Assert.NotNull(freshLoan);
        decimal totalDue = freshLoan.TotalAmountDue;

        // Process partial repayment
        decimal firstInstallmentTotal = freshLoan.Installments[0].TotalDue;
        var partResult = await service.ProcessRepaymentAsync(loan.Id, firstInstallmentTotal);

        Assert.False(partResult.IsLoanClosed);
        Assert.True(partResult.PrincipalPaid > 0);
        Assert.True(partResult.InterestPaid > 0);

        // Pay remaining balance in full
        var fullResult = await service.ProcessRepaymentAsync(loan.Id, freshLoan.Installments[1].TotalDue);

        Assert.True(fullResult.IsLoanClosed);
        Assert.Equal(0.00m, fullResult.RemainingBalance);

        var closedLoan = await service.GetLoanByIdAsync(loan.Id);
        Assert.Equal(LoanStatus.Closed, closedLoan!.Status);

        // Verify interest income registered in GL account 4100
        var interestIncomeAcc = await db.LedgerAccounts.FirstAsync(a => a.AccountNumber == "4100");
        Assert.True(interestIncomeAcc.Balance > 0m);
    }
}
