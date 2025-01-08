using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using PockitBook.Services;
using PockitBook.ViewModels;
using Dapper;
using PockitBook.Models;

namespace PockitBook.IntegrationTests;

/// <summary>
/// Integration tests for the BillDetailsViewModel.
/// </summary>
public class BillDetailsViewModelTests
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public BillDetailsViewModelTests()
    {
        _serviceProvider = TestStartUp.BuildTestServiceProvider("pockitBookTest.db");
        _dbConnector = _serviceProvider.GetRequiredService<DataBaseConnector>();
        _mainWindowViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
    }

    private DataBaseConnector _dbConnector;
    private ServiceProvider _serviceProvider;
    private MainWindowViewModel _mainWindowViewModel;

    /// <summary>
    /// Tests that the AddBill() method is successfully and correctly writing to the database.
    /// </summary>
    [Fact]
    public void AddBill_ValidInputs_SuccessfulWrites()
    {
        // Assign

        // Keep the connection open until the end of the test, otherwise
        // the in-memory database wipes out. 
        using var connection = new SqliteConnection(_dbConnector._connectionString);
        connection.Open();

        _dbConnector.InitializeDataBase();

        var nameOfNewBill = "MyTestBill";
        var dueDay = "21";
        var viewModel = new BillDetailsViewModel(_mainWindowViewModel, _dbConnector)
        {
            NameOfNewBill = nameOfNewBill,
            DueDay = dueDay
        };

        // Act
        viewModel.AddBill();

        // Assert
        var basicBills = connection
            .Query<BasicBillModel>("SELECT * FROM basicbills;")
            .ToList();

        Assert.True(basicBills.Count == 1);
        Assert.True(basicBills[0].Name == nameOfNewBill);
        Assert.True(basicBills[0].DueDayOfMonth == int.Parse(dueDay));
    }

    /// <summary>
    /// Tests that the AddBill() method unsuccessfully writes to the database due to invalid values.
    /// </summary>
    [Fact]
    public void AddBill_InvalidInputs_UnsuccessfulWrites()
    {
        // Assign

        // Keep the connection open until the end of the test, otherwise
        // the in-memory database wipes out. 
        using var connection = new SqliteConnection(_dbConnector._connectionString);
        connection.Open();

        _dbConnector.InitializeDataBase();

        var nameOfNewBill = "MyTestBill";
        var dueDay = "3131";
        var viewModel = new BillDetailsViewModel(_mainWindowViewModel, _dbConnector)
        {
            NameOfNewBill = nameOfNewBill,
            DueDay = dueDay
        };

        // Act
        viewModel.AddBill();

        // Assert
        var basicBills = connection
            .Query<BasicBillModel>("SELECT * FROM basicbills;")
            .ToList();

        Assert.True(basicBills.Count == 0);
    }
}