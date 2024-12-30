using System;
using System.Data.SQLite;

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
    public DataBaseConnector(string dbName)
    {
        _connectionString = new SQLiteConnectionStringBuilder()
        {
            DataSource = dbName,
        }.ToString();
    }

    private readonly string _connectionString;

    public void Test()
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =
        @"
                -- Create the table if it doesn't already exist
                CREATE TABLE IF NOT EXISTS mytesttable (
                    PersonName TEXT
                    );

                -- Insert records into the table
                INSERT INTO mytesttable (PersonName) 
                VALUES 
                ('James'),
                ('Read'),
                ('Joe'), 
                ('Phon');

                -- Select all records from the table
                SELECT * FROM mytesttable;
            ";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var name = reader.GetString(0);
            Console.WriteLine($"Hello {name}");
        }
    }
}