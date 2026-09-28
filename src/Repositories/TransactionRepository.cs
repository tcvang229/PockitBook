using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using PockitBook.Models;

namespace PockitBook.Repositories;

/// <summary>
/// Handles reading and writing Transaction records (the actual, historical "track record").
/// </summary>
public class TransactionRepository
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public TransactionRepository(SqliteDatabase database, ILogger<TransactionRepository> logger)
    {
        _database = database;
        _logger = logger;
    }

    private readonly SqliteDatabase _database;
    private readonly ILogger<TransactionRepository> _logger;

    /// <summary>
    /// Adds a transaction to the database.
    /// </summary>
    public async Task<int> AddAsync(Transaction transaction)
    {
        using var connection = new SqliteConnection(_database._connectionString);
        await connection.OpenAsync();

        const string insertStatement =
            """
                INSERT INTO transactions
                    (account_id, date, amount, description, category, scheduled_item_id, source, external_id, dedupe_hash, dedupe_hash_version, import_batch_id)
                VALUES
                    (@AccountId, @Date, @Amount, @Description, @Category, @ScheduledItemId, @Source, @ExternalId, @DedupeHash, @DedupeHashVersion, @ImportBatchId);
            """;

        try
        {
            return await connection.ExecuteAsync(insertStatement, transaction);
        }
        catch (Exception e)
        {
            string serializedModel = JsonSerializer.Serialize(transaction);
            _logger.LogError(e, "Failed to insert into `transactions`. Model:\n{Model}", serializedModel);
            return 0;
        }
    }

    /// <summary>
    /// Returns transactions for the given account, on or before the given date, ordered by date.
    /// </summary>
    public async Task<IEnumerable<Transaction>> GetByAccountThroughDateAsync(int accountId, DateTime throughDate)
    {
        using var connection = new SqliteConnection(_database._connectionString);
        await connection.OpenAsync();

        const string sqlCommand =
            """
                SELECT
                    id as Id,
                    account_id as AccountId,
                    date as Date,
                    amount as Amount,
                    description as Description,
                    category as Category,
                    scheduled_item_id as ScheduledItemId,
                    source as Source,
                    external_id as ExternalId,
                    dedupe_hash as DedupeHash,
                    dedupe_hash_version as DedupeHashVersion,
                    import_batch_id as ImportBatchId
                FROM
                    transactions
                WHERE
                    account_id = @AccountId AND date <= @ThroughDate
                ORDER BY
                    date ASC
            """;

        try
        {
            return await connection.QueryAsync<Transaction>(sqlCommand, new { AccountId = accountId, ThroughDate = throughDate });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to select records from `transactions` table.");
            return [];
        }
    }

    /// <summary>
    /// Returns the sum of transaction amounts for an account, after (exclusive) one date and
    /// through (inclusive) another. Used to compute a balance forward from a checkpoint anchor.
    /// </summary>
    public async Task<decimal> GetSumAsync(int accountId, DateTime afterDate, DateTime throughDate)
    {
        using var connection = new SqliteConnection(_database._connectionString);
        await connection.OpenAsync();

        // Summed in .NET rather than via SQL SUM() - SQLite's arithmetic is double-precision,
        // which would reintroduce float rounding error for money that decimal is meant to avoid.
        const string sqlCommand =
            """
                SELECT amount
                FROM transactions
                WHERE account_id = @AccountId AND date > @AfterDate AND date <= @ThroughDate
            """;

        try
        {
            IEnumerable<decimal> amounts = await connection.QueryAsync<decimal>(
                sqlCommand,
                new { AccountId = accountId, AfterDate = afterDate, ThroughDate = throughDate });
            return amounts.Sum();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to sum `transactions` table.");
            return 0m;
        }
    }

    /// <summary>
    /// Returns the set of dedupe hashes already recorded for an account, used to skip duplicate
    /// rows during a CSV import without querying once per row.
    /// </summary>
    public async Task<HashSet<string>> GetExistingDedupeHashesAsync(int accountId)
    {
        using var connection = new SqliteConnection(_database._connectionString);
        await connection.OpenAsync();

        const string sqlCommand =
            """
                SELECT dedupe_hash
                FROM transactions
                WHERE account_id = @AccountId AND dedupe_hash IS NOT NULL
            """;

        try
        {
            IEnumerable<string> hashes = await connection.QueryAsync<string>(sqlCommand, new { AccountId = accountId });
            return hashes.ToHashSet();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to select dedupe hashes from `transactions` table.");
            return [];
        }
    }
}
