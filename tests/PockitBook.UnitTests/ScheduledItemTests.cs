using PockitBook.Models;

namespace PockitBook.UnitTests;

/// <summary>
/// Unit tests for ScheduledItem's computed properties, which exist specifically to make the
/// Bill Details grid's cells directly and safely editable.
/// </summary>
public class ScheduledItemTests
{
    private static ScheduledItem BuildItem(DateTime anchorDate) => new()
    {
        AccountId = 1,
        Name = "Test",
        Type = ScheduledItemType.Bill,
        ExpectedAmount = 10m,
        Recurrence = RecurrenceType.Monthly,
        AnchorDate = anchorDate,
        StartDate = anchorDate,
        IsActive = true
    };

    [Fact]
    public void DueDay_Get_ReturnsAnchorDateDay()
    {
        var item = BuildItem(new DateTime(2026, 3, 15));

        Assert.Equal(15, item.DueDay);
    }

    [Fact]
    public void DueDay_Set_RebuildsAnchorDateWithSameYearMonth()
    {
        var item = BuildItem(new DateTime(2026, 3, 15));

        item.DueDay = 22;

        Assert.Equal(new DateTime(2026, 3, 22), item.AnchorDate);
    }

    [Fact]
    public void DueDay_SetPast31InShorterMonth_ClampsToLastDayOfMonth()
    {
        var item = BuildItem(new DateTime(2026, 2, 1)); // February 2026 has 28 days

        item.DueDay = 31;

        Assert.Equal(28, item.DueDay);
        Assert.Equal(new DateTime(2026, 2, 28), item.AnchorDate);
    }

    [Fact]
    public void DueDay_SetBelowOne_ClampsToFirstOfMonth()
    {
        var item = BuildItem(new DateTime(2026, 3, 15));

        item.DueDay = 0;

        Assert.Equal(1, item.DueDay);
    }

    [Fact]
    public void ExpectedAmount_SetNegative_StoresAsPositiveMagnitude()
    {
        var item = BuildItem(DateTime.Today);

        item.ExpectedAmount = -50m;

        Assert.Equal(50m, item.ExpectedAmount);
    }
}
