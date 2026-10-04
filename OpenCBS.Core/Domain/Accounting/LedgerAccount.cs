using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenCBS.Core.Domain.Accounting;

public enum AccountCategory
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Revenue = 4,
    Expense = 5
}

public class LedgerAccount
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string AccountNumber { get; set; } = string.Empty; // e.g., "1010" Cash, "1200" Loans, "2100" Deposits

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public AccountCategory Category { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; } = 0.00m;

    public bool IsActive { get; set; } = true;

    [MaxLength(250)]
    public string Description { get; set; } = string.Empty;
}
