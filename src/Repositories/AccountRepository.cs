using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using PockitBook.Models;

namespace PockitBook.Repositories;

/// <summary>
/// Handles reading and writing Account records.
/// </summary>
public class AccountRepository
{
    /// <summary>
    /// The three real accounts covered by the v1 CSV import scope (Wells Fargo Format A).
    /// </summary>
    public static readonly string[] SeedAccountNames = ["Checking", "Savings", "Credit Card"];

    /// <summary>
    /// Constructor.
    /// </summary>
    public AccountRepository(SqliteDatabase database, ILogger<AccountRepository> logger)
    {
        _database = database;
        _logger = logger;
    }

    private readonly SqliteDatabase _database;
    private readonly ILogger<AccountRepository> _logger;

    /// <summary>
    /// Inserts the seed accounts (Checking, Savings, Credit Card) if the accounts table is empty.
    /// </summary>
    public async Task EnsureSeedAccountsAsync()
    {
        using var connection = new SqliteConnection(_database._connectionString);
        await connection.OpenAsync();

        try
        {
            int existingCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM accounts;");
            if (existingCount > 0)
                return;

            const string insertStatement =
                """
                    INSERT INTO accounts (name, type, created_at)
                    VALUES (@Name, @Type, @CreatedAt);
                """;

            DateTime now = DateTime.UtcNow;
            var seedAccounts = new[]
            {
                new { Name = SeedAccountNames[0], Type = (int)AccountType.Checking, CreatedAt = now },
                new { Name = SeedAccountNames[1], Type = (int)AccountType.Savings, CreatedAt = now },
                new { Name = SeedAccountNames[2], Type = (int)AccountType.Credit, CreatedAt = now }
            };

            await connection.ExecuteAsync(insertStatement, seedAccounts);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to seed accounts.");
        }
    }

    /// <summary>
    /// Returns all accounts.
    /// </summary>
    public async Task<IEnumerable<Account>> GetAllAsync()
    {
        using var connection = new SqliteConnection(_database._connectionString);
        await connection.OpenAsync();

        const string sqlCommand =
            """
                SELECT
                    id as Id,
                    name as Name,
                    type as Type,
                    created_at as CreatedAt
                FROM
                    accounts
            """;

        try
        {
            return await connection.QueryAsync<Account>(sqlCommand);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to select records from `accounts` table.");
            return [];
        }
    }

    /// <summary>
    /// Returns the account with the given name, or null if it doesn't exist yet.
    /// </summary>
    public async Task<Account?> GetByNameAsync(string name)
    {
        IEnumerable<Account> accounts = await GetAllAsync();
        return accounts.FirstOrDefault(a => a.Name == name);
    }
}
