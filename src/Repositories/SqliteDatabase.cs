using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace PockitBook.Repositories;

/// <summary>
/// Class that handles SQLite database connection and schema setup.
/// </summary>
public class SqliteDatabase
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="dbName"></param>
    /// <param name="logger"></param>
    /// <param name="isTesting"></param>
    public SqliteDatabase(string dbName, ILogger<SqliteDatabase> logger, bool isTesting = false)
    {
        // Todo: could probably set this up in appsettings.json. could then manipulate what appsettings.json file
        // to use during testing vs production vs local development
        _connectionString = new SqliteConnectionStringBuilder()
        {
            DataSource = dbName,
            Mode = isTesting ? SqliteOpenMode.Memory : SqliteOpenMode.ReadWriteCreate,
            Cache = isTesting ? SqliteCacheMode.Shared : SqliteCacheMode.Default
        }.ToString();

        _logger = logger;
    }

    internal readonly string _connectionString;
    private ILogger<SqliteDatabase> _logger;

    /// <summary>
    /// Initial database setup. Drops the legacy `basic_bills` table (superseded by
    /// `scheduled_items`) and creates the current schema.
    /// </summary>
    /// <returns></returns>
    public async Task<Exception?> InitializeDataBaseAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // TODO: build a SQL versioning system. this is too manual
        const string createTablesStatement =
            """
                DROP TABLE IF EXISTS basic_bills;

                CREATE TABLE IF NOT EXISTS accounts
                    (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL,
                        type INTEGER NOT NULL,
                        created_at TEXT NOT NULL
                    );

                CREATE TABLE IF NOT EXISTS import_batches
                    (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        account_id INTEGER NOT NULL REFERENCES accounts(id),
                        imported_at TEXT NOT NULL,
                        file_name TEXT NOT NULL,
                        row_count INTEGER NOT NULL,
                        duplicate_count INTEGER NOT NULL
                    );

                CREATE TABLE IF NOT EXISTS scheduled_items
                    (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        account_id INTEGER NOT NULL REFERENCES accounts(id),
                        name TEXT NOT NULL,
                        type INTEGER NOT NULL,
                        expected_amount TEXT NOT NULL,
                        recurrence INTEGER NOT NULL,
                        anchor_date TEXT NOT NULL,
                        start_date TEXT NOT NULL,
                        end_date TEXT,
                        category TEXT,
                        is_active INTEGER NOT NULL
                    );

                CREATE TABLE IF NOT EXISTS transactions
                    (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        account_id INTEGER NOT NULL REFERENCES accounts(id),
                        date TEXT NOT NULL,
                        amount TEXT NOT NULL,
                        description TEXT NOT NULL,
                        category TEXT,
                        scheduled_item_id INTEGER REFERENCES scheduled_items(id),
                        source INTEGER NOT NULL,
                        external_id TEXT,
                        dedupe_hash TEXT,
                        dedupe_hash_version INTEGER,
                        import_batch_id INTEGER REFERENCES import_batches(id)
                    );

                CREATE TABLE IF NOT EXISTS balance_checkpoints
                    (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        account_id INTEGER NOT NULL REFERENCES accounts(id),
                        date TEXT NOT NULL,
                        balance TEXT NOT NULL,
                        source INTEGER NOT NULL,
                        note TEXT
                    );
            """;

        try
        {
            await connection.ExecuteAsync(createTablesStatement);
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to initialize database schema. SQL command:\n{@SqlCommandtext}", createTablesStatement);
            return e;
        }
    }
}
