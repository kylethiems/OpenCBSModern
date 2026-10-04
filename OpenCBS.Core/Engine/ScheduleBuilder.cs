using OpenCBS.Core.Domain.Loans;

namespace OpenCBS.Core.Engine;

public class ScheduleBuilder
{
    /// <summary>
    /// Generates the full loan amortization schedule with mathematical precision.
    /// Handles Annuity, Declining Balance, and Flat interest methodologies.
    /// </summary>
    public static List<LoanInstallment> BuildSchedule(
        decimal principal,
        decimal annualRate,
        int installmentCount,
        DateTime startDate,
        InterestCalculationMethod method,
        int gracePeriodMonths = 0)
    {
        if (principal <= 0)
            throw new ArgumentException("Principal must be greater than zero.", nameof(principal));
        if (installmentCount <= 0)
            throw new ArgumentException("Installment count must be greater than zero.", nameof(installmentCount));
        if (annualRate < 0)
            throw new ArgumentException("Interest rate cannot be negative.", nameof(annualRate));
        if (gracePeriodMonths < 0 || gracePeriodMonths >= installmentCount)
            throw new ArgumentException("Grace period must be non-negative and less than total installment count.", nameof(gracePeriodMonths));

        var installments = new List<LoanInstallment>(installmentCount);
        decimal monthlyRate = annualRate / 12m;
        int activeRepaymentPeriods = installmentCount - gracePeriodMonths;

        switch (method)
        {
            case InterestCalculationMethod.Annuity:
                BuildAnnuitySchedule(installments, principal, monthlyRate, installmentCount, gracePeriodMonths, startDate);
                break;

            case InterestCalculationMethod.DecliningBalance:
                BuildDecliningBalanceSchedule(installments, principal, monthlyRate, installmentCount, gracePeriodMonths, startDate);
                break;

            case InterestCalculationMethod.Flat:
                BuildFlatSchedule(installments, principal, annualRate, installmentCount, gracePeriodMonths, startDate);
                break;

            default:
                throw new NotSupportedException($"Method {method} is not supported.");
        }

        return installments;
    }

    private static void BuildAnnuitySchedule(
        List<LoanInstallment> list,
        decimal principal,
        decimal monthlyRate,
        int totalInstallments,
        int gracePeriod,
        DateTime startDate)
    {
        int repaymentPeriods = totalInstallments - gracePeriod;
        decimal outstandingBalance = principal;

        // PMT = P * [r(1+r)^n] / [(1+r)^n - 1]
        decimal monthlyPmt = 0m;
        if (monthlyRate > 0)
        {
            double r = (double)monthlyRate;
            double factor = Math.Pow(1.0 + r, repaymentPeriods);
            double pmt = (double)principal * (r * factor) / (factor - 1.0);
            monthlyPmt = Math.Round((decimal)pmt, 2, MidpointRounding.AwayFromZero);
        }
        else
        {
            monthlyPmt = Math.Round(principal / repaymentPeriods, 2, MidpointRounding.AwayFromZero);
        }

        decimal accumulatedPrincipal = 0m;

        for (int i = 1; i <= totalInstallments; i++)
        {
            var dueDate = startDate.AddMonths(i);
            decimal interest = Math.Round(outstandingBalance * monthlyRate, 2, MidpointRounding.AwayFromZero);
            decimal principalDue;

            if (i <= gracePeriod)
            {
                // Grace period: pay only interest
                principalDue = 0m;
            }
            else if (i == totalInstallments)
            {
                // Last installment absorbs any cumulative penny rounding differences
                principalDue = principal - accumulatedPrincipal;
            }
            else
            {
                principalDue = monthlyPmt - interest;
                if (principalDue < 0m) principalDue = 0m;
                if (accumulatedPrincipal + principalDue > principal)
                {
                    principalDue = principal - accumulatedPrincipal;
                }
            }

            accumulatedPrincipal += principalDue;
            outstandingBalance -= principalDue;

            list.Add(new LoanInstallment
            {
                InstallmentNumber = i,
                DueDate = dueDate,
                PrincipalDue = principalDue,
                InterestDue = interest,
                TotalDue = principalDue + interest
            });
        }
    }

    private static void BuildDecliningBalanceSchedule(
        List<LoanInstallment> list,
        decimal principal,
        decimal monthlyRate,
        int totalInstallments,
        int gracePeriod,
        DateTime startDate)
    {
        int repaymentPeriods = totalInstallments - gracePeriod;
        decimal constantPrincipal = Math.Round(principal / repaymentPeriods, 2, MidpointRounding.AwayFromZero);
        decimal outstandingBalance = principal;
        decimal accumulatedPrincipal = 0m;

        for (int i = 1; i <= totalInstallments; i++)
        {
            var dueDate = startDate.AddMonths(i);
            decimal interest = Math.Round(outstandingBalance * monthlyRate, 2, MidpointRounding.AwayFromZero);
            decimal principalDue;

            if (i <= gracePeriod)
            {
                principalDue = 0m;
            }
            else if (i == totalInstallments)
            {
                principalDue = principal - accumulatedPrincipal;
            }
            else
            {
                principalDue = constantPrincipal;
            }

            accumulatedPrincipal += principalDue;
            outstandingBalance -= principalDue;

            list.Add(new LoanInstallment
            {
                InstallmentNumber = i,
                DueDate = dueDate,
                PrincipalDue = principalDue,
                InterestDue = interest,
                TotalDue = principalDue + interest
            });
        }
    }

    private static void BuildFlatSchedule(
        List<LoanInstallment> list,
        decimal principal,
        decimal annualRate,
        int totalInstallments,
        int gracePeriod,
        DateTime startDate)
    {
        int repaymentPeriods = totalInstallments - gracePeriod;
        decimal totalInterest = Math.Round(principal * annualRate * (totalInstallments / 12.0m), 2, MidpointRounding.AwayFromZero);
        decimal monthlyInterest = Math.Round(totalInterest / totalInstallments, 2, MidpointRounding.AwayFromZero);
        decimal monthlyPrincipal = Math.Round(principal / repaymentPeriods, 2, MidpointRounding.AwayFromZero);

        decimal accumulatedPrincipal = 0m;
        decimal accumulatedInterest = 0m;

        for (int i = 1; i <= totalInstallments; i++)
        {
            var dueDate = startDate.AddMonths(i);
            decimal principalDue;
            decimal interestDue;

            if (i <= gracePeriod)
            {
                principalDue = 0m;
            }
            else if (i == totalInstallments)
            {
                principalDue = principal - accumulatedPrincipal;
            }
            else
            {
                principalDue = monthlyPrincipal;
            }

            if (i == totalInstallments)
            {
                interestDue = totalInterest - accumulatedInterest;
            }
            else
            {
                interestDue = monthlyInterest;
            }

            accumulatedPrincipal += principalDue;
            accumulatedInterest += interestDue;

            list.Add(new LoanInstallment
            {
                InstallmentNumber = i,
                DueDate = dueDate,
                PrincipalDue = principalDue,
                InterestDue = interestDue,
                TotalDue = principalDue + interestDue
            });
        }
    }
}
