using System.ComponentModel.DataAnnotations.Schema;

namespace OpenCBS.Core.Domain.Loans;

public class LoanInstallment
{
    public int Id { get; set; }

    public int LoanContractId { get; set; }
    public LoanContract? LoanContract { get; set; }

    public int InstallmentNumber { get; set; }

    public DateTime DueDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PrincipalDue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal InterestDue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalDue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidPrincipal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidInterest { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidTotal { get; set; }

    public bool IsPaid { get; set; } = false;

    public DateTime? PaidDateUtc { get; set; }

    [NotMapped]
    public decimal RemainingPrincipal => PrincipalDue - PaidPrincipal;

    [NotMapped]
    public decimal RemainingInterest => InterestDue - PaidInterest;

    [NotMapped]
    public decimal RemainingTotal => TotalDue - PaidTotal;
}
