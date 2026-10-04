using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenCBS.Core.Domain.Savings;

public enum SavingsTransactionType
{
    Deposit = 1,
    Withdrawal = 2,
    InterestAccrual = 3,
    Fee = 4
}

public class SavingsTransaction
{
    public int Id { get; set; }

    public int SavingsAccountId { get; set; }
    public SavingsAccount? SavingsAccount { get; set; }

    public SavingsTransactionType TransactionType { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BalanceAfter { get; set; }

    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
