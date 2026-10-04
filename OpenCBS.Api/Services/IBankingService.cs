using OpenCBS.Core.Domain.Accounting;
using OpenCBS.Core.Domain.Clients;
using OpenCBS.Core.Domain.Loans;
using OpenCBS.Core.Domain.Savings;

namespace OpenCBS.Api.Services;

public record CreateClientDto(
    string IdentificationNumber,
    string FirstName,
    string LastName,
    string CompanyName,
    ClientType ClientType,
    string Email,
    string PhoneNumber,
    string Address);

public record ApplyLoanDto(
    int ClientId,
    decimal PrincipalAmount,
    decimal AnnualInterestRate,
    int InstallmentCount,
    int GracePeriodMonths,
    InterestCalculationMethod CalculationMethod);

public record RepaymentResult(
    int LoanId,
    decimal AmountPaid,
    decimal PrincipalPaid,
    decimal InterestPaid,
    decimal RemainingBalance,
    bool IsLoanClosed);

public record SavingsTransactionDto(
    int AccountId,
    decimal Amount,
    string Description);

public record PostJournalEntryDto(
    string Description,
    List<JournalLineDto> Lines);

public record JournalLineDto(
    string AccountNumber,
    decimal DebitAmount,
    decimal CreditAmount,
    string Memo);

public record TrialBalanceItem(
    string AccountNumber,
    string AccountName,
    AccountCategory Category,
    decimal Balance);

public interface IBankingService
{
    // Clients
    Task<Client> CreateClientAsync(CreateClientDto dto);
    Task<Client?> GetClientByIdAsync(int id);
    Task<List<Client>> GetAllClientsAsync();
    Task<Client> UpdateKycStatusAsync(int clientId, ClientStatus status);

    // Loans
    Task<LoanContract> ApplyForLoanAsync(ApplyLoanDto dto);
    Task<LoanContract?> GetLoanByIdAsync(int id);
    Task<List<LoanContract>> GetLoansByClientAsync(int clientId);
    Task<LoanContract> ApproveLoanAsync(int loanId);
    Task<LoanContract> DisburseLoanAsync(int loanId);
    Task<RepaymentResult> ProcessRepaymentAsync(int loanId, decimal paymentAmount);

    // Savings
    Task<SavingsAccount> OpenSavingsAccountAsync(int clientId, SavingsAccountType type, decimal initialDeposit, decimal overdraftLimit = 0m);
    Task<SavingsAccount?> GetSavingsAccountByIdAsync(int id);
    Task<List<SavingsAccount>> GetSavingsAccountsByClientAsync(int clientId);
    Task<SavingsAccount> DepositAsync(int accountId, decimal amount, string description = "Cash Deposit");
    Task<SavingsAccount> WithdrawAsync(int accountId, decimal amount, string description = "Cash Withdrawal");

    // Accounting / General Ledger
    Task<JournalEntry> PostJournalEntryAsync(PostJournalEntryDto dto);
    Task<List<TrialBalanceItem>> GetTrialBalanceAsync();
}
