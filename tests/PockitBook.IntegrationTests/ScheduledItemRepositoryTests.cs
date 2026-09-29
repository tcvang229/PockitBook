using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using PockitBook.Models;
using PockitBook.Repositories;

namespace PockitBook.IntegrationTests;

/// <summary>
/// Integration tests for ScheduledItemRepository's update/delete methods, which back the Bill
/// Details grid's inline cell editing and per-row delete button.
/// </summary>
public class ScheduledItemRepositoryTests
{
    public ScheduledItemRepositoryTests()
    {
        var serviceProvider = TestStartUp.BuildTestServiceProvider("pockitBookScheduledItemTest.db");
        _database = serviceProvider.GetRequiredService<SqliteDatabase>();
        _accountRepository = serviceProvider.GetRequiredService<AccountRepository>();
        _scheduledItemRepository = serviceProvider.GetRequiredService<ScheduledItemRepository>();
    }

    private readonly SqliteDatabase _database;
    private readonly AccountRepository _accountRepository;
    private readonly ScheduledItemRepository _scheduledItemRepository;

    private async Task<(SqliteConnection Connection, int AccountId)> SetupAsync()
    {
        var connection = new SqliteConnection(_database._connectionString);
        await connection.OpenAsync();

        await _database.InitializeDataBaseAsync();
        await _accountRepository.EnsureSeedAccountsAsync();

        Account? account = await _accountRepository.GetByNameAsync(AccountRepository.SeedAccountNames[0]);
        return (connection, account!.Id!.Value);
    }

    private static ScheduledItem BuildItem(int accountId, string name, decimal amount, int dueDay) => new()
    {
        AccountId = accountId,
        Name = name,
        Type = ScheduledItemType.Bill,
        ExpectedAmount = amount,
        Recurrence = RecurrenceType.Monthly,
        AnchorDate = new DateTime(2026, 6, dueDay),
        StartDate = new DateTime(2026, 6, 1),
        IsActive = true
    };

    [Fact]
    public async Task UpdateAsync_ChangedFields_PersistToDatabase()
    {
        var (connection, accountId) = await SetupAsync();
        using SqliteConnection _ = connection;

        await _scheduledItemRepository.AddAsync(BuildItem(accountId, "Rent", 1200m, 1));
        ScheduledItem added = (await _scheduledItemRepository.GetByAccountAsync(accountId)).Single();

        added.Name = "Rent (updated)";
        added.ExpectedAmount = 1300m;
        added.DueDay = 5;
        added.Type = ScheduledItemType.Income;

        int rowsAffected = await _scheduledItemRepository.UpdateAsync(added);

        ScheduledItem reloaded = (await _scheduledItemRepository.GetByAccountAsync(accountId)).Single();
        Assert.Equal(1, rowsAffected);
        Assert.Equal("Rent (updated)", reloaded.Name);
        Assert.Equal(1300m, reloaded.ExpectedAmount);
        Assert.Equal(5, reloaded.DueDay);
        Assert.Equal(ScheduledItemType.Income, reloaded.Type);
    }

    [Fact]
    public async Task AddAsync_BiweeklyItemWithDateAdjustment_RoundTripsThroughDatabase()
    {
        var (connection, accountId) = await SetupAsync();
        using SqliteConnection _ = connection;

        var salary = new ScheduledItem
        {
            AccountId = accountId,
            Name = "Salary",
            Type = ScheduledItemType.Income,
            ExpectedAmount = 3098.78m,
            Recurrence = RecurrenceType.Biweekly,
            AnchorDate = new DateTime(2026, 9, 18),
            StartDate = new DateTime(2026, 9, 18),
            IsActive = true,
            DateAdjustment = DateAdjustmentRule.NearestPriorBusinessDay
        };

        await _scheduledItemRepository.AddAsync(salary);
        ScheduledItem reloaded = (await _scheduledItemRepository.GetByAccountAsync(accountId)).Single();

        Assert.Equal(RecurrenceType.Biweekly, reloaded.Recurrence);
        Assert.Equal(DateAdjustmentRule.NearestPriorBusinessDay, reloaded.DateAdjustment);
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyTheSpecifiedRow()
    {
        var (connection, accountId) = await SetupAsync();
        using SqliteConnection _ = connection;

        await _scheduledItemRepository.AddAsync(BuildItem(accountId, "Rent", 1200m, 1));
        await _scheduledItemRepository.AddAsync(BuildItem(accountId, "Paycheck", 2500m, 15));

        List<ScheduledItem> beforeDelete = (await _scheduledItemRepository.GetByAccountAsync(accountId)).ToList();
        ScheduledItem toDelete = beforeDelete.Single(i => i.Name == "Rent");

        await _scheduledItemRepository.DeleteAsync(toDelete.Id!.Value);

        List<ScheduledItem> afterDelete = (await _scheduledItemRepository.GetByAccountAsync(accountId)).ToList();
        Assert.Single(afterDelete);
        Assert.Equal("Paycheck", afterDelete[0].Name);
    }
}
