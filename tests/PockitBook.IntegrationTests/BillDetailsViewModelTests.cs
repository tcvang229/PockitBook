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
        var amountDue = "3.7";
        var viewModel = new BillDetailsViewModel(_mainWindowViewModel, _dbConnector)
        {
            NameOfNewBill = nameOfNewBill,
            DueDay = dueDay,
            AmountDue = amountDue
        };

        // Act
        await viewModel.AddBillAsync();

        // Assert
        var basicBills = connection
            .Query<BasicBillModel>(
                """
                    SELECT 
                        name as Name,
                        due_day_of_month as DueDayOfMonth,
                        amount_due as AmountDue
                    FROM 
                        basic_bills;
                """
                )
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
            .Query<BasicBillModel>("SELECT * FROM basic_bills;")
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
                DueDayOfMonth = 1,
                AmountDue = 1
            },
            new BasicBillModel()
            {
                Name = "TestBill2",
                DueDayOfMonth = 2,
                AmountDue = 3.0f
            },
            new BasicBillModel()
            {
                Name = "TestBill3",
                DueDayOfMonth = 3,
                AmountDue = 3.3f
            },
        };

        // TODO: Could automate this better?
        const string sqlCommand =
        """
            INSERT INTO 
                basic_bills (name, due_day_of_month, amount_due)
            VALUES
                ('TestBill1', 1, 1),
                ('TestBill2', 2, 3.0),
                ('TestBill3', 3, 3.3);
        """;

        await connection.ExecuteAsync(sqlCommand);

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