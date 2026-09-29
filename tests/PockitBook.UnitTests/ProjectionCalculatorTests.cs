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
        bool isActive = true,
        DateAdjustmentRule dateAdjustment = DateAdjustmentRule.None) => new()
        {
            AccountId = 1,
            Name = "Test Item",
            Type = type,
            ExpectedAmount = amount,
            Recurrence = recurrence,
            AnchorDate = anchorDate,
            StartDate = startDate ?? anchorDate,
            EndDate = endDate,
            IsActive = isActive,
            DateAdjustment = dateAdjustment
        };

    [Fact]
    public void BuildProjection_MonthlyBill_SubtractsOnEachOccurrence()
    {
        var windowStart = new DateTime(2026, 1, 1);
        var windowEnd = new DateTime(2026, 3, 31);
        var item = BuildItem(ScheduledItemType.Bill, 100m, RecurrenceType.Monthly, new DateTime(2026, 1, 15));

        var sut = new ProjectionCalculator();
        List<LabeledDateTimePoint> points = sut.BuildProjection(1000m, windowStart, windowEnd, [item]);

        // Starting point + Jan/Feb/Mar occurrences
        Assert.Equal(4, points.Count);
        Assert.Equal(900d, points[1].Value);
        Assert.Equal(800d, points[2].Value);
        Assert.Equal(700d, points[3].Value);
        Assert.Equal("Starting balance", points[0].Label);
        Assert.Equal("Test Item", points[1].Label);
        Assert.Null(points[0].Delta);
        Assert.Equal(-100m, points[1].Delta);
    }

    [Fact]
    public void BuildProjection_MonthlyIncome_AddsOnEachOccurrence()
    {
        var windowStart = new DateTime(2026, 1, 1);
        var windowEnd = new DateTime(2026, 2, 28);
        var item = BuildItem(ScheduledItemType.Income, 500m, RecurrenceType.Monthly, new DateTime(2026, 1, 15));

        var sut = new ProjectionCalculator();
        List<LabeledDateTimePoint> points = sut.BuildProjection(1000m, windowStart, windowEnd, [item]);

        Assert.Equal(1500d, points[1].Value);
        Assert.Equal(2000d, points[2].Value);
        Assert.Equal(500m, points[1].Delta);
    }

    [Fact]
    public void BuildProjection_MonthlyAnchorDay31_ClampsToShorterMonth()
    {
        var windowStart = new DateTime(2026, 1, 1);
        var windowEnd = new DateTime(2026, 2, 28);
        var item = BuildItem(ScheduledItemType.Bill, 50m, RecurrenceType.Monthly, new DateTime(2026, 1, 31));

        var sut = new ProjectionCalculator();
        List<LabeledDateTimePoint> points = sut.BuildProjection(1000m, windowStart, windowEnd, [item]);

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
        List<LabeledDateTimePoint> points = sut.BuildProjection(100m, windowStart, windowEnd, [item]);

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
        List<LabeledDateTimePoint> points = sut.BuildProjection(0m, windowStart, windowEnd, [item]);

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
        List<LabeledDateTimePoint> points = sut.BuildProjection(1000m, windowStart, windowEnd, [item]);

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
        List<LabeledDateTimePoint> points = sut.BuildProjection(1000m, windowStart, windowEnd, [item]);

        Assert.Single(points);
        Assert.Equal(1000d, points[0].Value);
    }

    [Fact]
    public void BuildProjection_BiweeklySaturdayWithNearestPriorBusinessDay_ShiftsToFriday()
    {
        // 2026-01-03 is a Saturday.
        var windowStart = new DateTime(2026, 1, 1);
        var windowEnd = new DateTime(2026, 1, 3);
        var item = BuildItem(
            ScheduledItemType.Income,
            3000m,
            RecurrenceType.Biweekly,
            new DateTime(2026, 1, 3),
            dateAdjustment: DateAdjustmentRule.NearestPriorBusinessDay);

        var sut = new ProjectionCalculator();
        List<LabeledDateTimePoint> points = sut.BuildProjection(0m, windowStart, windowEnd, [item]);

        Assert.Equal(2, points.Count);
        Assert.Equal(new DateTime(2026, 1, 2), points[1].DateTime);
    }

    [Fact]
    public void BuildProjection_BiweeklySundayWithNearestPriorBusinessDay_ShiftsToFriday()
    {
        // 2026-01-04 is a Sunday.
        var windowStart = new DateTime(2026, 1, 1);
        var windowEnd = new DateTime(2026, 1, 4);
        var item = BuildItem(
            ScheduledItemType.Income,
            3000m,
            RecurrenceType.Biweekly,
            new DateTime(2026, 1, 4),
            dateAdjustment: DateAdjustmentRule.NearestPriorBusinessDay);

        var sut = new ProjectionCalculator();
        List<LabeledDateTimePoint> points = sut.BuildProjection(0m, windowStart, windowEnd, [item]);

        Assert.Equal(2, points.Count);
        Assert.Equal(new DateTime(2026, 1, 2), points[1].DateTime);
    }

    [Fact]
    public void BuildProjection_BiweeklyWeekendWithNoDateAdjustment_LeavesDateUnchanged()
    {
        // 2026-01-03 is a Saturday.
        var windowStart = new DateTime(2026, 1, 1);
        var windowEnd = new DateTime(2026, 1, 3);
        var item = BuildItem(
            ScheduledItemType.Income,
            3000m,
            RecurrenceType.Biweekly,
            new DateTime(2026, 1, 3),
            dateAdjustment: DateAdjustmentRule.None);

        var sut = new ProjectionCalculator();
        List<LabeledDateTimePoint> points = sut.BuildProjection(0m, windowStart, windowEnd, [item]);

        Assert.Equal(2, points.Count);
        Assert.Equal(new DateTime(2026, 1, 3), points[1].DateTime);
    }

    [Fact]
    public void BuildProjection_RepeatedWeekendShifts_DoNotDriftTheFourteenDayCadence()
    {
        // 2026-01-03 and 2026-01-17 are both Saturdays, 14 days apart - the shift should apply
        // independently to each occurrence rather than compounding across the recurrence.
        var windowStart = new DateTime(2026, 1, 1);
        var windowEnd = new DateTime(2026, 1, 17);
        var item = BuildItem(
            ScheduledItemType.Income,
            3000m,
            RecurrenceType.Biweekly,
            new DateTime(2026, 1, 3),
            dateAdjustment: DateAdjustmentRule.NearestPriorBusinessDay);

        var sut = new ProjectionCalculator();
        List<LabeledDateTimePoint> points = sut.BuildProjection(0m, windowStart, windowEnd, [item]);

        Assert.Equal(3, points.Count);
        Assert.Equal(new DateTime(2026, 1, 2), points[1].DateTime);
        Assert.Equal(new DateTime(2026, 1, 16), points[2].DateTime);
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
        List<LabeledDateTimePoint> points = sut.BuildProjection(1000m, windowStart, windowEnd, [item]);

        // Only the January occurrence falls before the EndDate.
        Assert.Equal(2, points.Count);
        Assert.Equal(900d, points[1].Value);
    }
}
