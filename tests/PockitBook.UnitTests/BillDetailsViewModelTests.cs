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

        return new BillDetailsViewModel(iScreen, accountRepository, scheduledItemRepository);
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
}
