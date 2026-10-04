using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenCBS.Core.Domain.Accounting;

public class JournalEntry
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string ReferenceNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Description { get; set; } = string.Empty;

    public DateTime PostingDateUtc { get; set; } = DateTime.UtcNow;

    public List<JournalEntryLine> Lines { get; set; } = new();

    [NotMapped]
    public decimal TotalDebits => Lines.Sum(l => l.DebitAmount);

    [NotMapped]
    public decimal TotalCredits => Lines.Sum(l => l.CreditAmount);

    [NotMapped]
    public bool IsBalanced => Math.Abs(TotalDebits - TotalCredits) < 0.0001m;
}

public class JournalEntryLine
{
    public int Id { get; set; }

    public int JournalEntryId { get; set; }
    public JournalEntry? JournalEntry { get; set; }

    public int LedgerAccountId { get; set; }
    public LedgerAccount? LedgerAccount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DebitAmount { get; set; } = 0.00m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal CreditAmount { get; set; } = 0.00m;

    [MaxLength(200)]
    public string Memo { get; set; } = string.Empty;
}
