using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using PockitBook.Models;
using PockitBook.Services;
using PockitBook.Repositories;

namespace PockitBook.IntegrationTests;

/// <summary>
/// Integration tests for BalanceCheckpointRepository's computed-balance formula: the most
/// recent checkpoint at or before a date, plus transactions since that checkpoint through
/// that date.
/// </summary>
public class BalanceCheckpointRepositoryTests
{
    public BalanceCheckpointRepositoryTests()
    {
        var serviceProvider = TestStartUp.BuildTestServiceProvider("pockitBookBalanceTest.db");
        _database = serviceProvider.GetRequiredService<SqliteDatabase>();
        _accountRepository = serviceProvider.GetRequiredService<AccountRepository>();
        _transactionRepository = serviceProvider.GetRequiredService<TransactionRepository>();
        _balanceCheckpointRepository = serviceProvider.GetRequiredService<BalanceCheckpointRepository>();
    }

    private readonly SqliteDatabase _database;
    private readonly AccountRepository _accountRepository;
    private readonly TransactionRepository _transactionRepository;
    private readonly BalanceCheckpointRepository _balanceCheckpointRepository;

    /// <summary>
    /// Opens (and keeps open, for the caller to dispose) the connection that pins the shared
    /// in-memory test database alive, then sets up the schema and returns the primary account id.
    /// </summary>
    private async Task<(SqliteConnection Connection, int AccountId)> SetupAsync()
    {
        var connection = new SqliteConnection(_database._connectionString);
        await connection.OpenAsync();

        await _database.InitializeDataBaseAsync();
        await _accountRepository.EnsureSeedAccountsAsync();

        Account? account = await _accountRepository.GetByNameAsync(AccountRepository.SeedAccountNames[0]);
        return (connection, account!.Id!.Value);
    }

    private static Transaction BuildTransaction(int accountId, DateTime date, decimal amount) => new()
    {
        AccountId = accountId,
        Date = date,
        Amount = amount,
        Description = "Test transaction",
        Source = TransactionSource.Manual
    };

    [Fact]
    public async Task GetComputedBalanceAsync_NoCheckpoint_SumsAllTransactions()
    {
        var (connection, accountId) = await SetupAsync();
        using SqliteConnection _ = connection;

        var today = new DateTime(2026, 6, 1);
        await _transactionRepository.AddAsync(BuildTransaction(accountId, today.AddDays(-2), 100m));
        await _transactionRepository.AddAsync(BuildTransaction(accountId, today.AddDays(-1), -30m));

        decimal balance = await _balanceCheckpointRepository.GetComputedBalanceAsync(accountId, today);

        Assert.Equal(70m, balance);
    }

    [Fact]
    public async Task GetComputedBalanceAsync_WithCheckpoint_AnchorsFromCheckpoint()
    {
        var (connection, accountId) = await SetupAsync();
        using SqliteConnection _ = connection;

        var checkpointDate = new DateTime(2026, 6, 1);
        var today = new DateTime(2026, 6, 10);

        await _balanceCheckpointRepository.AddAsync(new BalanceCheckpoint
        {
            AccountId = accountId,
            Date = checkpointDate,
            Balance = 500m,
            Source = TransactionSource.Manual
        });

        // Before the checkpoint - should not affect the computed balance.
        await _transactionRepository.AddAsync(BuildTransaction(accountId, checkpointDate.AddDays(-5), 9000m));

        // After the checkpoint, through today.
        await _transactionRepository.AddAsync(BuildTransaction(accountId, checkpointDate.AddDays(2), -50m));
        await _transactionRepository.AddAsync(BuildTransaction(accountId, checkpointDate.AddDays(4), 20m));

        decimal balance = await _balanceCheckpointRepository.GetComputedBalanceAsync(accountId, today);

        Assert.Equal(470m, balance);
    }

    [Fact]
    public async Task GetComputedBalanceAsync_OverrideCheckpoint_SupersedesEarlierOne()
    {
        var (connection, accountId) = await SetupAsync();
        using SqliteConnection _ = connection;

        var firstCheckpointDate = new DateTime(2026, 6, 1);
        var overrideCheckpointDate = new DateTime(2026, 6, 5);
        var today = new DateTime(2026, 6, 10);

        await _balanceCheckpointRepository.AddAsync(new BalanceCheckpoint
        {
            AccountId = accountId,
            Date = firstCheckpointDate,
            Balance = 100m,
            Source = TransactionSource.CsvImport
        });

        // A manual correction supersedes the earlier checkpoint for any date on/after it.
        await _balanceCheckpointRepository.AddAsync(new BalanceCheckpoint
        {
            AccountId = accountId,
            Date = overrideCheckpointDate,
            Balance = 1000m,
            Source = TransactionSource.Manual,
            Note = "Found a missing transfer"
        });

        await _transactionRepository.AddAsync(BuildTransaction(accountId, overrideCheckpointDate.AddDays(1), 25m));

        decimal balance = await _balanceCheckpointRepository.GetComputedBalanceAsync(accountId, today);

        Assert.Equal(1025m, balance);
    }

    [Fact]
    public async Task GetBalanceHistoryAsync_NoCheckpoint_WalksFromZero()
    {
        var (connection, accountId) = await SetupAsync();
        using SqliteConnection _ = connection;

        var day1 = new DateTime(2026, 6, 1);
        var day2 = new DateTime(2026, 6, 5);
        await _transactionRepository.AddAsync(BuildTransaction(accountId, day1, 100m));
        await _transactionRepository.AddAsync(BuildTransaction(accountId, day2, -30m));

        var points = await _balanceCheckpointRepository.GetBalanceHistoryAsync(accountId, day2);

        Assert.Equal(3, points.Count);
        Assert.Equal(0d, points[0].Value);
        Assert.Equal(100d, points[1].Value);
        Assert.Equal(70d, points[2].Value);
    }

    [Fact]
    public async Task GetBalanceHistoryAsync_CheckpointBeforeTransactions_AnchorsFromCheckpoint()
    {
        var (connection, accountId) = await SetupAsync();
        using SqliteConnection _ = connection;

        var checkpointDate = new DateTime(2026, 6, 1);
        var today = new DateTime(2026, 6, 10);

        await _balanceCheckpointRepository.AddAsync(new BalanceCheckpoint
        {
            AccountId = accountId,
            Date = checkpointDate,
            Balance = 500m,
            Source = TransactionSource.Manual
        });
        await _transactionRepository.AddAsync(BuildTransaction(accountId, checkpointDate.AddDays(2), -50m));

        var points = await _balanceCheckpointRepository.GetBalanceHistoryAsync(accountId, today);

        Assert.Equal(2, points.Count);
        Assert.Equal(checkpointDate, points[0].DateTime);
        Assert.Equal(500d, points[0].Value);
        Assert.Equal(450d, points[^1].Value);
    }

    /// <summary>
    /// This is the exact bug found from a real run: importing historical transactions and then
    /// entering a manual balance override dated *after* all of them (e.g. "here's my real
    /// balance today"). The previous implementation only ever looked for a checkpoint at or
    /// before the *earliest* transaction, so a later override was silently ignored entirely.
    ///
    /// The fix goes further than just making the override "count": since we now know the true
    /// balance today (5000) and every transaction between then and the start of history, the
    /// true starting balance can be derived exactly by undoing each transaction in reverse -
    /// no more guessing it was zero.
    /// </summary>
    [Fact]
    public async Task GetBalanceHistoryAsync_CheckpointAfterTransactions_ReconstructsHistoryBackward()
    {
        var (connection, accountId) = await SetupAsync();
        using SqliteConnection _ = connection;

        var txDate1 = new DateTime(2026, 6, 1);
        var txDate2 = new DateTime(2026, 6, 5);
        var overrideDate = new DateTime(2026, 6, 10);

        await _transactionRepository.AddAsync(BuildTransaction(accountId, txDate1, 100m));
        await _transactionRepository.AddAsync(BuildTransaction(accountId, txDate2, -30m));

        await _balanceCheckpointRepository.AddAsync(new BalanceCheckpoint
        {
            AccountId = accountId,
            Date = overrideDate,
            Balance = 5000m,
            Source = TransactionSource.Manual,
            Note = "Manual override"
        });

        var points = await _balanceCheckpointRepository.GetBalanceHistoryAsync(accountId, overrideDate);

        // 5000 (true balance today) - (-30) on Jun 5 - 100 on Jun 1 = 4930 true starting balance.
        Assert.Equal(4, points.Count);
        Assert.Equal(txDate1, points[0].DateTime);
        Assert.Equal(4930d, points[0].Value);
        Assert.Equal(5030d, points[1].Value); // 4930 + 100 (Jun 1 transaction)
        Assert.Equal(5000d, points[2].Value); // 5030 - 30 (Jun 5 transaction)
        Assert.Equal(overrideDate, points[^1].DateTime);
        Assert.Equal(5000d, points[^1].Value); // matches - this override didn't need to correct drift
    }

    [Fact]
    public async Task GetBalanceHistoryAsync_CheckpointBetweenTransactions_ResetsMidTimeline()
    {
        var (connection, accountId) = await SetupAsync();
        using SqliteConnection _ = connection;

        var txBefore = new DateTime(2026, 6, 1);
        var checkpointDate = new DateTime(2026, 6, 5);
        var txAfter = new DateTime(2026, 6, 8);
        var throughDate = new DateTime(2026, 6, 10);

        await _transactionRepository.AddAsync(BuildTransaction(accountId, txBefore, 100m));
        await _balanceCheckpointRepository.AddAsync(new BalanceCheckpoint
        {
            AccountId = accountId,
            Date = checkpointDate,
            Balance = 9999m,
            Source = TransactionSource.Manual
        });
        await _transactionRepository.AddAsync(BuildTransaction(accountId, txAfter, 1m));

        var points = await _balanceCheckpointRepository.GetBalanceHistoryAsync(accountId, throughDate);

        // 9999 (checkpoint) - 100 (Jun 1 transaction, undone) = 9899 true starting balance.
        Assert.Equal(4, points.Count);
        Assert.Equal(9899d, points[0].Value); // reconstructed backward from the checkpoint
        Assert.Equal(9999d, points[1].Value); // 9899 + 100 (Jun 1 transaction) - matches the checkpoint exactly
        Assert.Equal(9999d, points[2].Value); // the checkpoint itself, on Jun 5
        Assert.Equal(10000d, points[3].Value); // transaction after the checkpoint applies on top of it
    }
}
