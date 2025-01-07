using PockitBook.ViewModels;
using NSubstitute;
using ReactiveUI;
using PockitBook.Services;
using Microsoft.Extensions.Logging;

namespace PockitBook.UnitTests;

/// <summary>
/// Unit tests for the Bill Details View Model.
/// </summary>
public class BillDetailsViewModelTests
{
    /// <summary>
    /// Tests that the TryBuildBasicBill method return false when the due day is out of range for realistic monthly days.
    /// </summary>
    /// <param name="dueDayOfMonth"></param>
    [Theory]
    [InlineData("319985")]
    [InlineData("32")]
    [InlineData("0")]
    [InlineData("-3")]
    public void TryBuildBasicBill_OutOfRangeDay_ReturnsFalse(string dueDayOfMonth)
    {
        // Assemble
        var iScreen = Substitute.For<IScreen>();
        var iLogger = Substitute.For<ILogger<DataBaseConnector>>();
        var dbConnector = Substitute.For<DataBaseConnector>("", iLogger);
        var sut = new BillDetailsViewModel(iScreen, dbConnector);

        // Act
        var result = sut.TryBuildBasicBill("testBill", dueDayOfMonth, out var createdBasicBill);

        // Assert
        Assert.False(result);
        Assert.Null(createdBasicBill);
    }

    /// <summary>
    /// Tests that the TryBuildBasicBill method returns true when the due day is within range of a realistic month.
    /// </summary>
    /// <param name="dueDayOfMonth"></param>
    [Theory]
    [InlineData("1")]
    [InlineData("20")]
    [InlineData("31")]
    public void TryBuildBasicBill_InRangeDay_ReturnsTrue(string dueDayOfMonth)
    {
        // Assemble
        var iScreen = Substitute.For<IScreen>();
        var iLogger = Substitute.For<ILogger<DataBaseConnector>>();
        var dbConnector = Substitute.For<DataBaseConnector>("", iLogger);
        var sut = new BillDetailsViewModel(iScreen, dbConnector);

        // Act
        var billName = "testBill";
        var result = sut.TryBuildBasicBill(billName, dueDayOfMonth, out var createdBasicBill);

        // Assert
        Assert.True(result);
        Assert.NotNull(createdBasicBill);
        Assert.Equal(billName, createdBasicBill.Name);

        var expectedDueDay = int.Parse(dueDayOfMonth);
        Assert.Equal(expectedDueDay, createdBasicBill.DueDayOfMonth);
    }
}