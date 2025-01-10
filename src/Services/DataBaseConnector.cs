using System;
using System.Collections.Generic;
using System.Linq;
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
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText =
        @"
            CREATE TABLE IF NOT EXISTS basicbills
                (
                    Name varchar(256) NOT NULL,
                    DueDayOfMonth int NOT NULL,
                    PRIMARY KEY (Name)
                );
        ";

        try
        {
            command.ExecuteNonQuery();
            return null;
        }
        catch (Exception e)
        {
            var loggingContext = new
            {
                SqlCommandText = command.CommandText
            };

            _logger.LogError(e, "Failed to execute SQL command.\n{@Context}", loggingContext);
            return e;
        }
    }

    /// <summary>
    /// Adds a Basic Bill to the database.
    /// </summary>
    /// <param name="bill"></param>
    /// <returns></returns>
    public async Task<Exception?> AddBasicBillAsync(BasicBillModel bill)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText =
        @"
            INSERT INTO basicbills
            VALUES ($billName, $dueDayOfMonth);
        ";
        command.Parameters.AddWithValue("$billName", bill.Name);
        command.Parameters.AddWithValue("$dueDayOfMonth", bill.DueDayOfMonth);

        try
        {
            await command.ExecuteNonQueryAsync();
            return null;
        }
        catch (Exception e)
        {
            var loggingContext = new
            {
                SqlCommandText = command.CommandText,
                Model = bill
            };

            _logger.LogError(e, "Failed to execute SQL command.\n{@Context}", loggingContext);
            return e;
        }
    }

    /// <summary>
    /// Returns a list of BasicBillsModels.
    /// </summary>
    /// <param name="basicBills"></param>
    /// <returns></returns>
    public async Task<(Exception? exception, List<BasicBillModel>? basicBillModels)> GetBasicBillsAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        try
        {
            var sqlCommand = "SELECT * FROM basicbills";
            var basicBills = (await connection // Does this return null if there are no records in table?
                .QueryAsync<BasicBillModel>(sqlCommand))
                .ToList();

            return (null, basicBills);
        }
        catch (Exception e)
        {
            return (e, null);
        }
    }
}