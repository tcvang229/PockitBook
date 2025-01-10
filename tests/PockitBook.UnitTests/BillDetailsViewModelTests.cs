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
        var dbConnector = Substitute.For<DataBaseConnector>("", iLogger, Arg.Any<bool>());
        var sut = new BillDetailsViewModel(iScreen, dbConnector);

        // Act
        var result = sut.TryBuildBasicBill("testBill", dueDayOfMonth);

        // Assert
        Assert.Null(result);
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
        var dbConnector = Substitute.For<DataBaseConnector>("", iLogger, Arg.Any<bool>());
        var sut = new BillDetailsViewModel(iScreen, dbConnector);

        // Act
        var billName = "testBill";
        var result = sut.TryBuildBasicBill(billName, dueDayOfMonth);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(billName, result.Name);

        var expectedDueDay = int.Parse(dueDayOfMonth);
        Assert.Equal(expectedDueDay, result.DueDayOfMonth);
    }
}