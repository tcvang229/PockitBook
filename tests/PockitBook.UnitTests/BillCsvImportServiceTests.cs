using Microsoft.Extensions.Logging;
using NSubstitute;
using PockitBook.Models;
using PockitBook.Services;
using PockitBook.Repositories;

namespace PockitBook.UnitTests;

/// <summary>
/// Unit tests for BillCsvImportService's row parsing. These don't touch a database -
/// ImportAsync's DB-writing orchestration is covered by integration tests instead.
/// </summary>
public class BillCsvImportServiceTests
{
    private const int TestAccountId = 1;

    private static BillCsvImportService BuildSut()
    {
        var databaseLogger = Substitute.For<ILogger<SqliteDatabase>>();
        var database = Substitute.For<SqliteDatabase>("", databaseLogger, false);
        var scheduledItemRepository = Substitute.For<ScheduledItemRepository>(database, Substitute.For<ILogger<ScheduledItemRepository>>());
        var logger = Substitute.For<ILogger<BillCsvImportService>>();

        return new BillCsvImportService(scheduledItemRepository, logger);
    }

    [Fact]
    public void ParseRows_ValidRows_ParsesNameDueDayAmountType()
    {
        var sut = BuildSut();
        string[] lines =
        [
            "Name,DueDay,Amount,Type",
            "Rent,1,2130,Bill",
            "Phone,4,100,Bill"
        ];

        var items = sut.ParseRows(lines, TestAccountId);

        Assert.Equal(2, items.Count);
        Assert.Equal("Rent", items[0].Name);
        Assert.Equal(1, items[0].AnchorDate.Day);
        Assert.Equal(2130m, items[0].ExpectedAmount);
        Assert.Equal(ScheduledItemType.Bill, items[0].Type);
        Assert.Equal(RecurrenceType.Monthly, items[0].Recurrence);
        Assert.True(items[0].IsActive);
        Assert.Equal(TestAccountId, items[0].AccountId);
    }

    [Fact]
    public void ParseRows_TypeColumnOmitted_DefaultsToBill()
    {
        var sut = BuildSut();
        string[] lines = ["Name,DueDay,Amount", "Rent,1,2130"];

        var items = sut.ParseRows(lines, TestAccountId);

        Assert.Single(items);
        Assert.Equal(ScheduledItemType.Bill, items[0].Type);
    }

    [Fact]
    public void ParseRows_IncomeType_IsCaseInsensitive()
    {
        var sut = BuildSut();
        string[] lines = ["Name,DueDay,Amount,Type", "Paycheck,15,2000,income"];

        var items = sut.ParseRows(lines, TestAccountId);

        Assert.Single(items);
        Assert.Equal(ScheduledItemType.Income, items[0].Type);
    }

    [Theory]
    [InlineData("Rent,0,2130,Bill")]
    [InlineData("Rent,32,2130,Bill")]
    [InlineData("Rent,not-a-day,2130,Bill")]
    [InlineData("Rent,1,not-a-number,Bill")]
    [InlineData(",1,2130,Bill")]
    public void ParseRows_InvalidRow_IsSkippedNotThrown(string invalidRow)
    {
        var sut = BuildSut();
        string[] lines = ["Name,DueDay,Amount,Type", invalidRow, "Phone,4,100,Bill"];

        var items = sut.ParseRows(lines, TestAccountId);

        Assert.Single(items);
        Assert.Equal("Phone", items[0].Name);
    }

    [Fact]
    public void ParseRows_AnchorDayPastMonthEnd_ClampsToShorterMonth()
    {
        var sut = BuildSut();
        string[] lines = ["Name,DueDay,Amount,Type", "Rent,31,2130,Bill"];
        DateTime today = DateTime.Today;
        int daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);

        var items = sut.ParseRows(lines, TestAccountId);

        Assert.Single(items);
        Assert.Equal(daysInMonth, items[0].AnchorDate.Day);
    }

    [Fact]
    public void ParseRows_EmptyFile_ReturnsEmpty()
    {
        var sut = BuildSut();
        string[] lines = ["Name,DueDay,Amount,Type"];

        var items = sut.ParseRows(lines, TestAccountId);

        Assert.Empty(items);
    }
}
