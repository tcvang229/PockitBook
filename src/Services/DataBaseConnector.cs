using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using PockitBook.Models;

namespace PockitBook.Services;

/// <summary>
/// Class that handles SQLite database connection.
/// </summary>
public class DataBaseConnector
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="dbName"></param>
    /// <param name="logger"></param>
    /// <param name="isTesting"></param>
    public DataBaseConnector(string dbName, ILogger<DataBaseConnector> logger, bool isTesting = false)
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
    private ILogger<DataBaseConnector> _logger;

    /// <summary>
    /// Initial database setup.
    /// </summary>
    /// <returns></returns>
    public async Task<Exception?> InitializeDataBaseAsync()
    {
        // Todo: delete me once we have good database versioning/rollbacks
        // await DropBasicBillsTable();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // TODO: build a SQL versioning system. this is too manual
        const string createTableStatement =
            """
                CREATE TABLE IF NOT EXISTS basic_bills
                    (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name VARCHAR(256) NOT NULL,
                        due_day_of_month INTEGER NOT NULL,
                        amount_due REAL NOT NULL
                    );
            """;

        try
        {
            await connection.ExecuteAsync(createTableStatement);
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to initialize `basicbills`. SQL command:\n{@SqlCommandtext}", createTableStatement);
            return e;
        }
    }

    /// <summary>
    /// Adds a Basic Bill to the database.
    /// </summary>
    /// <param name="bill"></param>Context
    /// <returns></returns>
    public async Task<int> AddBasicBillAsync(BasicBillModel bill)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        const string insertStatement =
            """
                INSERT INTO basic_bills (name, due_day_of_month, amount_due)
                VALUES ($Name, $DueDayOfMonth, $AmountDue);
            """;

        try
        {
            int rowsAffected = await connection.ExecuteAsync(insertStatement, bill);
            return rowsAffected;
        }
        catch (Exception e)
        {
            string serializedModel = JsonSerializer.Serialize(bill);

            _logger.LogError
                (
                    e,
                    "Failed to insert records into `basicbills`. SQL command:\n{SqlCommandtext}\nModel:\n{Model}",
                    insertStatement,
                    serializedModel
                );

            return 0;
        }
    }

    /// <summary>
    /// Returns a list of BasicBillsModels.
    /// </summary>
    /// <param name="basicBills"></param>
    /// <returns></returns>
    public async Task<IEnumerable<BasicBillModel>?> GetBasicBillsAsync()
    {
        // Todo: Support pagination, this can get really big

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        const string sqlCommand =
            """
                SELECT 
                    id as Id,
                    name as Name,
                    due_day_of_month as DueDayOfMonth,
                    amount_due as AmountDue
                FROM 
                    basic_bills
            """;

        try
        {

            IEnumerable<BasicBillModel> basicBills = await connection.QueryAsync<BasicBillModel>(sqlCommand);

            return basicBills;
        }
        catch (Exception e)
        {
            _logger.LogError
                (
                    e,
                    "Failed to select records from `basicbills` table. SQL command: \n{SqlCommandtext}",
                    sqlCommand
                );

            return null;
        }
    }

    private async Task DropBasicBillsTable()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        const string sqlCommand =
            """
                DROP TABLE IF EXISTS basic_bills
            """;

        try
        {
            await connection.ExecuteAsync(sqlCommand);
        }
        catch (Exception exception)
        {
            _logger.LogError
                (
                    exception,
                    "Failed to drop `basicbills` table. SQL command:\n{SqlCommandtext}",
                    sqlCommand
                );
        }
    }
}