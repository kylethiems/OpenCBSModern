using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenCBS.Core.Domain.Clients;

namespace OpenCBS.Core.Domain.Loans;

public enum LoanStatus
{
    PendingApproval = 0,
    Approved = 1,
    Disbursed = 2,
    Closed = 3,
    Defaulted = 4
}

public enum InterestCalculationMethod
{
    Annuity = 1,          // Equal total installment (standard amortization)
    DecliningBalance = 2, // Constant principal + declining interest on outstanding balance
    Flat = 3              // Interest calculated on initial principal across all periods
}

public class LoanContract
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string ContractCode { get; set; } = string.Empty;

    public int ClientId { get; set; }
    public Client? Client { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PrincipalAmount { get; set; }

    [Column(TypeName = "decimal(8,4)")]
    public decimal AnnualInterestRate { get; set; } // e.g. 0.1200 for 12%

    public int InstallmentCount { get; set; }

    public int GracePeriodMonths { get; set; } = 0;

    public InterestCalculationMethod CalculationMethod { get; set; } = InterestCalculationMethod.Annuity;

    public LoanStatus Status { get; set; } = LoanStatus.PendingApproval;

    public DateTime? DisbursementDateUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<LoanInstallment> Installments { get; set; } = new();

    [NotMapped]
    public decimal TotalPrincipalDue => Installments.Sum(i => i.PrincipalDue);

    [NotMapped]
    public decimal TotalInterestDue => Installments.Sum(i => i.InterestDue);

    [NotMapped]
    public decimal TotalAmountDue => Installments.Sum(i => i.TotalDue);

    [NotMapped]
    public decimal TotalPaid => Installments.Sum(i => i.PaidTotal);

    [NotMapped]
    public decimal OutstandingBalance => TotalAmountDue - TotalPaid;
}
