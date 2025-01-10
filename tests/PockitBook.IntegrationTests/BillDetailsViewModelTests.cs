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
    public async Task AddBill_ValidInputs_SuccessfulWrites()
    {
        // Assign

        // Keep the connection open until the end of the test, otherwise
        // the in-memory database wipes out. 
        using var connection = new SqliteConnection(_dbConnector._connectionString);
        await connection.OpenAsync();

        await _dbConnector.InitializeDataBaseAsync();

        var nameOfNewBill = "MyTestBill";
        var dueDay = "21";
        var viewModel = new BillDetailsViewModel(_mainWindowViewModel, _dbConnector)
        {
            NameOfNewBill = nameOfNewBill,
            DueDay = dueDay
        };

        // Act
        await viewModel.AddBillAsync();

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
    public async Task AddBill_InvalidInputs_UnsuccessfulWrites()
    {
        // Assign

        // Keep the connection open until the end of the test, otherwise
        // the in-memory database wipes out. 
        using var connection = new SqliteConnection(_dbConnector._connectionString);
        await connection.OpenAsync();

        await _dbConnector.InitializeDataBaseAsync();

        var nameOfNewBill = "MyTestBill";
        var dueDay = "3131";
        var viewModel = new BillDetailsViewModel(_mainWindowViewModel, _dbConnector)
        {
            NameOfNewBill = nameOfNewBill,
            DueDay = dueDay
        };

        // Act
        await viewModel.AddBillAsync();

        // Assert
        var basicBills = connection
            .Query<BasicBillModel>("SELECT * FROM basicbills;")
            .ToList();

        Assert.True(basicBills.Count == 0);
    }

    /// <summary>
    /// Tests that the SetBasicBillsAsync() method sets the basic bills list correctly within the view model.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task SetBasicBillsAsync_SuccessfulQuery()
    {
        // Assign
        using var connection = new SqliteConnection(_dbConnector._connectionString);
        await connection.OpenAsync();

        await _dbConnector.InitializeDataBaseAsync();

        var testBasicBills = new List<BasicBillModel>()
        {
            new BasicBillModel()
            {
                Name = "TestBill1",
                DueDayOfMonth = 1
            },
            new BasicBillModel()
            {
                Name = "TestBill2",
                DueDayOfMonth = 2
            },
            new BasicBillModel()
            {
                Name = "TestBill3",
                DueDayOfMonth = 3
            },
        };

        // TODO: Could automate this better?
        using var command = connection.CreateCommand();
        command.CommandText =
        $@"
            INSERT INTO basicbills
            VALUES
            ('TestBill1', 1),
            ('TestBill2', 2),
            ('TestBill3', 3);
        ";

        Console.WriteLine(command.CommandText);

        await command.ExecuteNonQueryAsync();

        var viewModel = new BillDetailsViewModel(_mainWindowViewModel, _dbConnector);

        // Act
        await viewModel.SetBasicBillsAsync();

        // Assert
        Assert.Equal(testBasicBills.Count, viewModel.BasicBills.Count);
        for (var i = 0; i < testBasicBills.Count; i++)
        {
            Assert.Equal(testBasicBills[i].Name, viewModel.BasicBills[i].Name);
            Assert.Equal(testBasicBills[i].DueDayOfMonth, viewModel.BasicBills[i].DueDayOfMonth);
        }
    }
}