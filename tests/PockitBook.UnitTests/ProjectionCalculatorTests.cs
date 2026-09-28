using LiveChartsCore.Defaults;
using PockitBook.Models;
using PockitBook.Services;

namespace PockitBook.UnitTests;

/// <summary>
/// Unit tests for ProjectionCalculator's recurrence expansion.
/// </summary>
public class ProjectionCalculatorTests
{
    private static ScheduledItem BuildItem(
        ScheduledItemType type,
        decimal amount,
        RecurrenceType recurrence,
        DateTime anchorDate,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool isActive = true) => new()
        {
            AccountId = 1,
            Name = "Test Item",
            Type = type,
            ExpectedAmount = amount,
            Recurrence = recurrence,
            AnchorDate = anchorDate,
            StartDate = startDate ?? anchorDate,
            EndDate = endDate,
            IsActive = isActive
        };

    [Fact]
    public void BuildProjection_MonthlyBill_SubtractsOnEachOccurrence()
    {
        var windowStart = new DateTime(2026, 1, 1);
        var windowEnd = new DateTime(2026, 3, 31);
        var item = BuildItem(ScheduledItemType.Bill, 100m, RecurrenceType.Monthly, new DateTime(2026, 1, 15));

        var sut = new ProjectionCalculator();
        List<DateTimePoint> points = sut.BuildProjection(1000m, windowStart, windowEnd, [item]);

        // Starting point + Jan/Feb/Mar occurrences
        Assert.Equal(4, points.Count);
        Assert.Equal(900d, points[1].Value);
        Assert.Equal(800d, points[2].Value);
        Assert.Equal(700d, points[3].Value);
    }

    [Fact]
    public void BuildProjection_MonthlyIncome_AddsOnEachOccurrence()
    {
        var windowStart = new DateTime(2026, 1, 1);
        var windowEnd = new DateTime(2026, 2, 28);
        var item = BuildItem(ScheduledItemType.Income, 500m, RecurrenceType.Monthly, new DateTime(2026, 1, 15));

        var sut = new ProjectionCalculator();
        List<DateTimePoint> points = sut.BuildProjection(1000m, windowStart, windowEnd, [item]);

        Assert.Equal(1500d, points[1].Value);
        Assert.Equal(2000d, points[2].Value);
    }

    [Fact]
    public void BuildProjection_MonthlyAnchorDay31_ClampsToShorterMonth()
    {
        var windowStart = new DateTime(2026, 1, 1);
        var windowEnd = new DateTime(2026, 2, 28);
        var item = BuildItem(ScheduledItemType.Bill, 50m, RecurrenceType.Monthly, new DateTime(2026, 1, 31));

        var sut = new ProjectionCalculator();
        List<DateTimePoint> points = sut.BuildProjection(1000m, windowStart, windowEnd, [item]);

        // February 2026 has 28 days - the 31st should clamp to the 28th instead of throwing.
        Assert.Equal(3, points.Count);
        Assert.Equal(new DateTime(2026, 1, 31), points[1].DateTime);
        Assert.Equal(new DateTime(2026, 2, 28), points[2].DateTime);
    }

    [Fact]
    public void BuildProjection_Weekly_RecursEverySevenDays()
    {
        var windowStart = new DateTime(2026, 1, 1);
        var windowEnd = new DateTime(2026, 1, 22);
        var item = BuildItem(ScheduledItemType.Bill, 10m, RecurrenceType.Weekly, new DateTime(2026, 1, 1));

        var sut = new ProjectionCalculator();
        List<DateTimePoint> points = sut.BuildProjection(100m, windowStart, windowEnd, [item]);

        // Occurrences on Jan 1, 8, 15, 22 (plus the starting point).
        Assert.Equal(5, points.Count);
        Assert.Equal(new DateTime(2026, 1, 22), points[^1].DateTime);
    }

    [Fact]
    public void BuildProjection_Biweekly_RecursEveryFourteenDays()
    {
        var windowStart = new DateTime(2026, 1, 1);
        var windowEnd = new DateTime(2026, 1, 29);
        var item = BuildItem(ScheduledItemType.Income, 200m, RecurrenceType.Biweekly, new DateTime(2026, 1, 1));

        var sut = new ProjectionCalculator();
        List<DateTimePoint> points = sut.BuildProjection(0m, windowStart, windowEnd, [item]);

        // Occurrences on Jan 1, 15, 29 (plus the starting point) - the anchor coincides with
        // windowStart, so it counts as a real occurrence rather than being absorbed into the
        // starting balance (consistent with the Weekly test below).
        Assert.Equal(4, points.Count);
        Assert.Equal(600d, points[^1].Value);
    }

    [Fact]
    public void BuildProjection_OneTime_OccursOnceOnAnchorDate()
    {
        var windowStart = new DateTime(2026, 1, 1);
        var windowEnd = new DateTime(2026, 3, 31);
        var item = BuildItem(ScheduledItemType.Bill, 250m, RecurrenceType.OneTime, new DateTime(2026, 2, 10));

        var sut = new ProjectionCalculator();
        List<DateTimePoint> points = sut.BuildProjection(1000m, windowStart, windowEnd, [item]);

        Assert.Equal(2, points.Count);
        Assert.Equal(750d, points[1].Value);
    }

    [Fact]
    public void BuildProjection_InactiveItem_IsExcluded()
    {
        var windowStart = new DateTime(2026, 1, 1);
        var windowEnd = new DateTime(2026, 3, 31);
        var item = BuildItem(ScheduledItemType.Bill, 100m, RecurrenceType.Monthly, new DateTime(2026, 1, 15), isActive: false);

        var sut = new ProjectionCalculator();
        List<DateTimePoint> points = sut.BuildProjection(1000m, windowStart, windowEnd, [item]);

        Assert.Single(points);
        Assert.Equal(1000d, points[0].Value);
    }

    [Fact]
    public void BuildProjection_PastEndDate_StopsProjecting()
    {
        var windowStart = new DateTime(2026, 1, 1);
        var windowEnd = new DateTime(2026, 3, 31);
        var item = BuildItem(
            ScheduledItemType.Bill,
            100m,
            RecurrenceType.Monthly,
            new DateTime(2026, 1, 15),
            endDate: new DateTime(2026, 1, 20));

        var sut = new ProjectionCalculator();
        List<DateTimePoint> points = sut.BuildProjection(1000m, windowStart, windowEnd, [item]);

        // Only the January occurrence falls before the EndDate.
        Assert.Equal(2, points.Count);
        Assert.Equal(900d, points[1].Value);
    }
}
