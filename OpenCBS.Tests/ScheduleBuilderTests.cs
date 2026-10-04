using OpenCBS.Core.Domain.Loans;
using OpenCBS.Core.Engine;

namespace OpenCBS.Tests;

public class ScheduleBuilderTests
{
    [Fact]
    public void AnnuitySchedule_PrincipalSum_MatchesOriginalPrincipalExactly()
    {
        // Arrange
        decimal principal = 10_000.00m;
        decimal annualRate = 0.12m; // 12%
        int installments = 12;
        DateTime start = new(2026, 1, 1);

        // Act
        var schedule = ScheduleBuilder.BuildSchedule(
            principal, annualRate, installments, start, InterestCalculationMethod.Annuity);

        // Assert
        Assert.Equal(12, schedule.Count);
        decimal sumPrincipal = schedule.Sum(i => i.PrincipalDue);
        Assert.Equal(principal, sumPrincipal);

        // First installment interest should be 10,000 * 0.12 / 12 = 100.00
        Assert.Equal(100.00m, schedule[0].InterestDue);
    }

    [Fact]
    public void AnnuitySchedule_WithGracePeriod_HasZeroPrincipalDuringGrace()
    {
        // Arrange
        decimal principal = 5_000.00m;
        decimal annualRate = 0.10m;
        int installments = 6;
        int gracePeriod = 2;

        // Act
        var schedule = ScheduleBuilder.BuildSchedule(
            principal, annualRate, installments, DateTime.UtcNow, InterestCalculationMethod.Annuity, gracePeriod);

        // Assert
        Assert.Equal(6, schedule.Count);
        Assert.Equal(0.00m, schedule[0].PrincipalDue);
        Assert.Equal(0.00m, schedule[1].PrincipalDue);
        Assert.True(schedule[2].PrincipalDue > 0m);
        Assert.Equal(principal, schedule.Sum(i => i.PrincipalDue));
    }

    [Fact]
    public void DecliningBalance_ConstantPrincipal_AndDecreasingInterest()
    {
        // Arrange
        decimal principal = 12_000.00m;
        decimal annualRate = 0.12m;
        int installments = 12;

        // Act
        var schedule = ScheduleBuilder.BuildSchedule(
            principal, annualRate, installments, DateTime.UtcNow, InterestCalculationMethod.DecliningBalance);

        // Assert
        Assert.Equal(12, schedule.Count);
        foreach (var inst in schedule)
        {
            Assert.Equal(1_000.00m, inst.PrincipalDue);
        }

        // Interest should strictly decrease as balance declines
        for (int i = 1; i < schedule.Count; i++)
        {
            Assert.True(schedule[i].InterestDue < schedule[i - 1].InterestDue);
        }

        Assert.Equal(principal, schedule.Sum(i => i.PrincipalDue));
    }

    [Fact]
    public void FlatSchedule_EqualInterestDistribution()
    {
        // Arrange
        decimal principal = 6_000.00m;
        decimal annualRate = 0.12m;
        int installments = 6;

        // Act
        var schedule = ScheduleBuilder.BuildSchedule(
            principal, annualRate, installments, DateTime.UtcNow, InterestCalculationMethod.Flat);

        // Assert
        Assert.Equal(6, schedule.Count);
        decimal sumPrincipal = schedule.Sum(i => i.PrincipalDue);
        decimal sumInterest = schedule.Sum(i => i.InterestDue);

        Assert.Equal(principal, sumPrincipal);
        Assert.True(sumInterest > 0m);

        // Flat interest is split equally
        decimal expectedMonthlyInterest = schedule[0].InterestDue;
        foreach (var inst in schedule.Take(5))
        {
            Assert.Equal(expectedMonthlyInterest, inst.InterestDue);
        }
    }

    [Fact]
    public void InvalidArguments_ThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            ScheduleBuilder.BuildSchedule(0m, 0.1m, 12, DateTime.UtcNow, InterestCalculationMethod.Annuity));

        Assert.Throws<ArgumentException>(() =>
            ScheduleBuilder.BuildSchedule(1000m, 0.1m, 0, DateTime.UtcNow, InterestCalculationMethod.Annuity));

        Assert.Throws<ArgumentException>(() =>
            ScheduleBuilder.BuildSchedule(1000m, 0.1m, 12, DateTime.UtcNow, InterestCalculationMethod.Annuity, gracePeriodMonths: 12));
    }
}
