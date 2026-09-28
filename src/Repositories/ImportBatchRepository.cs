using System;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using PockitBook.Models;

namespace PockitBook.Repositories;

/// <summary>
/// Handles reading and writing ImportBatch records - an audit trail for each CSV import.
/// </summary>
public class ImportBatchRepository
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public ImportBatchRepository(SqliteDatabase database, ILogger<ImportBatchRepository> logger)
    {
        _database = database;
        _logger = logger;
    }

    private readonly SqliteDatabase _database;
    private readonly ILogger<ImportBatchRepository> _logger;

    /// <summary>
    /// Adds an import batch record and returns its new id, or null if the insert failed.
    /// </summary>
    public async Task<int?> AddAsync(ImportBatch batch)
    {
        using var connection = new SqliteConnection(_database._connectionString);
        await connection.OpenAsync();

        const string insertStatement =
            """
                INSERT INTO import_batches (account_id, imported_at, file_name, row_count, duplicate_count)
                VALUES (@AccountId, @ImportedAt, @FileName, @RowCount, @DuplicateCount);
                SELECT last_insert_rowid();
            """;

        try
        {
            return await connection.ExecuteScalarAsync<int>(insertStatement, batch);
        }
        catch (Exception e)
        {
            string serializedModel = JsonSerializer.Serialize(batch);
            _logger.LogError(e, "Failed to insert into `import_batches`. Model:\n{Model}", serializedModel);
            return null;
        }
    }
}
