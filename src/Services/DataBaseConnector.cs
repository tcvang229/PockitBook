using System;
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
    /// Adds a Basic Bill to the database.
    /// </summary>
    /// <param name="bill"></param>
    /// <returns></returns>
    public bool AddBasicBill(BasicBillModel bill)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

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
            command.ExecuteScalar();
        }
        catch (Exception e)
        {
            var loggingContext = new
            {
                SqlCommandText = command.CommandText,
                Model = bill
            };

            _logger.LogError(e, "Failed to execute SQL command.\n{@Context}", loggingContext);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Initial database setup.
    /// </summary>
    /// <returns></returns>
    public bool InitializeDataBase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
        @"
            CREATE TABLE IF NOT EXISTS basicbills
                (
                    Name varchar(256),
                    DueDayOfMonth int
                );
        ";

        try
        {
            command.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            var loggingContext = new
            {
                SqlCommandText = command.CommandText
            };

            _logger.LogError(e, "Failed to execute SQL command.\n{@Context}", loggingContext);
            return false;
        }

        return true;
    }
}