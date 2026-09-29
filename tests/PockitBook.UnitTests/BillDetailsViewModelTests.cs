using PockitBook.ViewModels;
using NSubstitute;
using ReactiveUI;
using PockitBook.Models;
using PockitBook.Services;
using PockitBook.Repositories;
using Microsoft.Extensions.Logging;

namespace PockitBook.UnitTests;

/// <summary>
/// Unit tests for the Bill Details View Model.
/// </summary>
public class BillDetailsViewModelTests
{
    private const int TestAccountId = 1;

    private static BillDetailsViewModel BuildSut()
    {
        var iScreen = Substitute.For<IScreen>();
        var databaseLogger = Substitute.For<ILogger<SqliteDatabase>>();
        var database = Substitute.For<SqliteDatabase>("", databaseLogger, false);
        var accountRepository = Substitute.For<AccountRepository>(database, Substitute.For<ILogger<AccountRepository>>());
        var scheduledItemRepository = Substitute.For<ScheduledItemRepository>(database, Substitute.For<ILogger<ScheduledItemRepository>>());
        var billCsvImportService = Substitute.For<BillCsvImportService>(scheduledItemRepository, Substitute.For<ILogger<BillCsvImportService>>());

        return new BillDetailsViewModel(iScreen, accountRepository, scheduledItemRepository, billCsvImportService);
    }

    /// <summary>
    /// Tests that BuildScheduledItem returns null when the due day is out of range for realistic monthly days.
    /// </summary>
    /// <param name="dueDayOfMonth"></param>
    [Theory]
    [InlineData("319985")]
    [InlineData("32")]
    [InlineData("0")]
    [InlineData("-3")]
    public void BuildScheduledItem_OutOfRangeDay_ReturnsNull(string dueDayOfMonth)
    {
        // Assemble
        var sut = BuildSut();

        // Act
        var billName = "testBill";
        var result = sut.BuildScheduledItem(TestAccountId, billName, dueDayOfMonth, "3", ScheduledItemType.Bill);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that BuildScheduledItem returns a valid item when the due day is within range of a realistic month.
    /// </summary>
    /// <param name="dueDayOfMonth"></param>
    [Theory]
    [InlineData("1")]
    [InlineData("20")]
    [InlineData("28")]
    public void BuildScheduledItem_InRangeDay_ReturnsItem(string dueDayOfMonth)
    {
        // Assemble
        var sut = BuildSut();

        // Act
        var billName = "testBill";
        var result = sut.BuildScheduledItem(TestAccountId, billName, dueDayOfMonth, "3", ScheduledItemType.Bill);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(billName, result.Name);
        Assert.Equal(TestAccountId, result.AccountId);
        Assert.Equal(ScheduledItemType.Bill, result.Type);

        var expectedDueDay = int.Parse(dueDayOfMonth);
        Assert.Equal(expectedDueDay, result.AnchorDate.Day);
    }

    /// <summary>
    /// Tests that BuildScheduledItem returns null when the name is empty.
    /// </summary>
    [Fact]
    public void BuildScheduledItem_EmptyName_ReturnsNull()
    {
        var sut = BuildSut();

        var result = sut.BuildScheduledItem(TestAccountId, "  ", "15", "3", ScheduledItemType.Bill);

        Assert.Null(result);
    }

    /// <summary>
    /// Tests that BuildScheduledItem returns null when the amount isn't numeric.
    /// </summary>
    [Fact]
    public void BuildScheduledItem_InvalidAmount_ReturnsNull()
    {
        var sut = BuildSut();

        var result = sut.BuildScheduledItem(TestAccountId, "testBill", "15", "not-a-number", ScheduledItemType.Bill);

        Assert.Null(result);
    }

    /// <summary>
    /// Tests that BuildScheduledItem respects the Income type when given.
    /// </summary>
    [Fact]
    public void BuildScheduledItem_IncomeType_SetsTypeToIncome()
    {
        var sut = BuildSut();

        var result = sut.BuildScheduledItem(TestAccountId, "Paycheck", "15", "2000", ScheduledItemType.Income);

        Assert.NotNull(result);
        Assert.Equal(ScheduledItemType.Income, result.Type);
    }

    /// <summary>
    /// Tests that BuildScheduledItem defaults to Monthly (day-of-month parsing, no date
    /// adjustment) when no recurrence is passed - preserves the pre-recurrence-picker behavior
    /// for existing callers.
    /// </summary>
    [Fact]
    public void BuildScheduledItem_NoRecurrenceGiven_DefaultsToMonthlyWithNoDateAdjustment()
    {
        var sut = BuildSut();

        var result = sut.BuildScheduledItem(TestAccountId, "Rent", "15", "1200", ScheduledItemType.Bill);

        Assert.NotNull(result);
        Assert.Equal(RecurrenceType.Monthly, result.Recurrence);
        Assert.Equal(DateAdjustmentRule.None, result.DateAdjustment);
    }

    /// <summary>
    /// Tests the biweekly-salary scenario: a real anchor date (rather than a day-of-month) is
    /// parsed exactly, and Biweekly gets NearestPriorBusinessDay by default so a payday landing
    /// on a weekend still projects sensibly.
    /// </summary>
    [Fact]
    public void BuildScheduledItem_BiweeklyRecurrence_ParsesAnchorDateAndDefaultsDateAdjustment()
    {
        var sut = BuildSut();

        var result = sut.BuildScheduledItem(TestAccountId, "Salary", "09/18/2026", "3098.78", ScheduledItemType.Income, RecurrenceType.Biweekly);

        Assert.NotNull(result);
        Assert.Equal(RecurrenceType.Biweekly, result.Recurrence);
        Assert.Equal(new DateTime(2026, 9, 18), result.AnchorDate);
        Assert.Equal(DateAdjustmentRule.NearestPriorBusinessDay, result.DateAdjustment);
    }

    /// <summary>
    /// Tests that a non-Monthly recurrence with an unparseable date (rather than a day-of-month
    /// number) is rejected rather than silently misinterpreted.
    /// </summary>
    [Fact]
    public void BuildScheduledItem_BiweeklyRecurrence_InvalidDate_ReturnsNull()
    {
        var sut = BuildSut();

        var result = sut.BuildScheduledItem(TestAccountId, "Salary", "not-a-date", "3000", ScheduledItemType.Income, RecurrenceType.Biweekly);

        Assert.Null(result);
    }

    /// <summary>
    /// Tests that Monthly recurrence still rejects a full date string as a day-of-month input.
    /// </summary>
    [Fact]
    public void BuildScheduledItem_MonthlyRecurrence_DateStringInsteadOfDay_ReturnsNull()
    {
        var sut = BuildSut();

        var result = sut.BuildScheduledItem(TestAccountId, "Rent", "09/18/2026", "1200", ScheduledItemType.Bill, RecurrenceType.Monthly);

        Assert.Null(result);
    }
}
