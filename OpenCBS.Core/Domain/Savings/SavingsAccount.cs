using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenCBS.Core.Domain.Clients;

namespace OpenCBS.Core.Domain.Savings;

public enum SavingsAccountStatus
{
    Active = 1,
    Frozen = 2,
    Closed = 3
}

public enum SavingsAccountType
{
    DemandDeposit = 1, // Standard checking/savings with anytime access
    TermDeposit = 2    // Fixed deposit with maturity date
}

public class SavingsAccount
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string AccountNumber { get; set; } = string.Empty;

    public int ClientId { get; set; }
    public Client? Client { get; set; }

    public SavingsAccountType AccountType { get; set; } = SavingsAccountType.DemandDeposit;

    public SavingsAccountStatus Status { get; set; } = SavingsAccountStatus.Active;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; } = 0.00m;

    [Column(TypeName = "decimal(8,4)")]
    public decimal AnnualInterestRate { get; set; } = 0.0200m; // 2.0% annual interest

    public bool AllowOverdraft { get; set; } = false;

    [Column(TypeName = "decimal(18,2)")]
    public decimal OverdraftLimit { get; set; } = 0.00m;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<SavingsTransaction> Transactions { get; set; } = new();

    public decimal AvailableFunds => AllowOverdraft ? Balance + OverdraftLimit : Balance;
}
