using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using PockitBook.Models;

namespace PockitBook.Repositories;

/// <summary>
/// Handles reading and writing ScheduledItem records (anticipated bills/income).
/// </summary>
public class ScheduledItemRepository
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public ScheduledItemRepository(SqliteDatabase database, ILogger<ScheduledItemRepository> logger)
    {
        _database = database;
        _logger = logger;
    }

    private readonly SqliteDatabase _database;
    private readonly ILogger<ScheduledItemRepository> _logger;

    /// <summary>
    /// Adds a scheduled item to the database.
    /// </summary>
    public async Task<int> AddAsync(ScheduledItem item)
    {
        using var connection = new SqliteConnection(_database._connectionString);
        await connection.OpenAsync();

        const string insertStatement =
            """
                INSERT INTO scheduled_items
                    (account_id, name, type, expected_amount, recurrence, anchor_date, start_date, end_date, category, is_active)
                VALUES
                    (@AccountId, @Name, @Type, @ExpectedAmount, @Recurrence, @AnchorDate, @StartDate, @EndDate, @Category, @IsActive);
            """;

        try
        {
            return await connection.ExecuteAsync(insertStatement, item);
        }
        catch (Exception e)
        {
            string serializedModel = JsonSerializer.Serialize(item);
            _logger.LogError(e, "Failed to insert into `scheduled_items`. Model:\n{Model}", serializedModel);
            return 0;
        }
    }

    /// <summary>
    /// Returns the active scheduled items for the given account.
    /// </summary>
    public async Task<IEnumerable<ScheduledItem>> GetByAccountAsync(int accountId)
    {
        using var connection = new SqliteConnection(_database._connectionString);
        await connection.OpenAsync();

        const string sqlCommand =
            """
                SELECT
                    id as Id,
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
                    scheduled_items
                WHERE
                    account_id = @AccountId
            """;

        try
        {
            return await connection.QueryAsync<ScheduledItem>(sqlCommand, new { AccountId = accountId });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to select records from `scheduled_items` table.");
            return [];
        }
    }

    /// <summary>
    /// Updates an existing scheduled item's editable fields.
    /// </summary>
    public async Task<int> UpdateAsync(ScheduledItem item)
    {
        using var connection = new SqliteConnection(_database._connectionString);
        await connection.OpenAsync();

        const string updateStatement =
            """
                UPDATE scheduled_items
                SET
                    name = @Name,
                    type = @Type,
                    expected_amount = @ExpectedAmount,
                    recurrence = @Recurrence,
                    anchor_date = @AnchorDate,
                    start_date = @StartDate,
                    end_date = @EndDate,
                    category = @Category,
                    is_active = @IsActive
                WHERE
                    id = @Id
            """;

        try
        {
            return await connection.ExecuteAsync(updateStatement, item);
        }
        catch (Exception e)
        {
            string serializedModel = JsonSerializer.Serialize(item);
            _logger.LogError(e, "Failed to update `scheduled_items`. Model:\n{Model}", serializedModel);
            return 0;
        }
    }

    /// <summary>
    /// Deletes a single scheduled item.
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        using var connection = new SqliteConnection(_database._connectionString);
        await connection.OpenAsync();

        const string sqlCommand = "DELETE FROM scheduled_items WHERE id = @Id;";

        try
        {
            await connection.ExecuteAsync(sqlCommand, new { Id = id });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete record {Id} from `scheduled_items` table.", id);
        }
    }

    /// <summary>
    /// Deletes all scheduled items for the given account.
    /// </summary>
    public async Task DeleteAllAsync(int accountId)
    {
        using var connection = new SqliteConnection(_database._connectionString);
        await connection.OpenAsync();

        const string sqlCommand = "DELETE FROM scheduled_items WHERE account_id = @AccountId;";

        try
        {
            await connection.ExecuteAsync(sqlCommand, new { AccountId = accountId });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete records from `scheduled_items` table.");
        }
    }
}
