using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using PockitBook.Services;
using PockitBook.Repositories;
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
        _database = _serviceProvider.GetRequiredService<SqliteDatabase>();
        _accountRepository = _serviceProvider.GetRequiredService<AccountRepository>();
        _scheduledItemRepository = _serviceProvider.GetRequiredService<ScheduledItemRepository>();
        _mainWindowViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
    }

    private readonly SqliteDatabase _database;
    private readonly AccountRepository _accountRepository;
    private readonly ScheduledItemRepository _scheduledItemRepository;
    private readonly ServiceProvider _serviceProvider;
    private readonly MainWindowViewModel _mainWindowViewModel;

    /// <summary>
    /// Sets up the schema and seed accounts, keeping the connection open for the rest of the
    /// test so the in-memory database doesn't get wiped out, and returns the primary account id.
    /// </summary>
    private async Task<(SqliteConnection Connection, int AccountId)> SetupAsync()
    {
        var connection = new SqliteConnection(_database._connectionString);
        await connection.OpenAsync();

        await _database.InitializeDataBaseAsync();
        await _accountRepository.EnsureSeedAccountsAsync();

        Account? account = await _accountRepository.GetByNameAsync(AccountRepository.SeedAccountNames[0]);
        Assert.NotNull(account?.Id);

        return (connection, account!.Id!.Value);
    }

    /// <summary>
    /// Tests that the AddBillAsync() method is successfully and correctly writing to the database.
    /// </summary>
    [Fact]
    public async Task AddBillAsync_ValidInputs_SuccessfulWrites()
    {
        // Assign
        var (connection, accountId) = await SetupAsync();
        using SqliteConnection _ = connection;

        var nameOfNewBill = "MyTestBill";
        var dueDay = "21";
        var amountDue = "3.7";
        var viewModel = new BillDetailsViewModel(_mainWindowViewModel, _accountRepository, _scheduledItemRepository)
        {
            NameOfNewBill = nameOfNewBill,
            DueDay = dueDay,
            AmountDue = amountDue
        };
        await viewModel.InitializeAsync();

        // Act
        await viewModel.AddBillAsync();

        // Assert
        var scheduledItems = connection
            .Query<ScheduledItem>(
                """
                    SELECT
                        account_id as AccountId,
                        name as Name,
                        type as Type,
                        expected_amount as ExpectedAmount,
                        recurrence as Recurrence,
                        anchor_date as AnchorDate,
                        start_date as StartDate,
                        end_date as EndDate,
                        category as Category,
                        is_active as IsActive
                    FROM
                        scheduled_items;
                """
                )
            .ToList();

        Assert.True(scheduledItems.Count == 1);
        Assert.True(scheduledItems[0].Name == nameOfNewBill);
        Assert.True(scheduledItems[0].AnchorDate.Day == int.Parse(dueDay));
        Assert.True(scheduledItems[0].AccountId == accountId);
        Assert.Equal(string.Empty, viewModel.ValidationError);
    }

    /// <summary>
    /// Tests that the AddBillAsync() method unsuccessfully writes to the database due to invalid values.
    /// </summary>
    [Fact]
    public async Task AddBillAsync_InvalidInputs_UnsuccessfulWrites()
    {
        // Assign
        var (connection, _) = await SetupAsync();
        using SqliteConnection __ = connection;

        var nameOfNewBill = "MyTestBill";
        var dueDay = "3131";
        var viewModel = new BillDetailsViewModel(_mainWindowViewModel, _accountRepository, _scheduledItemRepository)
        {
            NameOfNewBill = nameOfNewBill,
            DueDay = dueDay
        };
        await viewModel.InitializeAsync();

        // Act
        await viewModel.AddBillAsync();

        // Assert
        var scheduledItems = connection.Query<ScheduledItem>("SELECT 1 FROM scheduled_items;").ToList();

        Assert.True(scheduledItems.Count == 0);
        Assert.NotEqual(string.Empty, viewModel.ValidationError);
    }

    /// <summary>
    /// Tests that the SetScheduledItemsAsync() method sets the scheduled items list correctly within the view model.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task SetScheduledItemsAsync_SuccessfulQuery()
    {
        // Assign
        var (connection, accountId) = await SetupAsync();
        using SqliteConnection _ = connection;

        var today = DateTime.Today.ToString("yyyy-MM-dd");

        // TODO: Could automate this better?
        const string sqlCommand =
        """
            INSERT INTO
                scheduled_items (account_id, name, type, expected_amount, recurrence, anchor_date, start_date, is_active)
            VALUES
                (@AccountId, 'TestBill1', 0, '1', 3, @Today, @Today, 1),
                (@AccountId, 'TestBill2', 0, '3.0', 3, @Today, @Today, 1),
                (@AccountId, 'TestBill3', 0, '3.3', 3, @Today, @Today, 1);
        """;

        await connection.ExecuteAsync(sqlCommand, new { AccountId = accountId, Today = today });

        var viewModel = new BillDetailsViewModel(_mainWindowViewModel, _accountRepository, _scheduledItemRepository);
        await viewModel.InitializeAsync();

        // Act
        await viewModel.SetScheduledItemsAsync();

        // Assert
        Assert.Equal(3, viewModel.ScheduledItems.Count);
        Assert.Equal("TestBill1", viewModel.ScheduledItems[0].Name);
        Assert.Equal("TestBill2", viewModel.ScheduledItems[1].Name);
        Assert.Equal("TestBill3", viewModel.ScheduledItems[2].Name);
    }
}
