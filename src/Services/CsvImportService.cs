using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PockitBook.Models;
using PockitBook.Repositories;

namespace PockitBook.Services;

/// <summary>
/// The outcome of a CSV import, used to show a summary to the user.
/// </summary>
public record CsvImportResult(int TotalRows, int PostedRows, int SkippedPendingRows, int ImportedRows, int DuplicateRows);

/// <summary>
/// Imports transactions from a Wells Fargo-format CSV export
/// (columns: "DATE","DESCRIPTION","AMOUNT","CHECK #","STATUS"). Only "Posted" rows are
/// imported - "Pending" rows can still change amount before they post, which would break the
/// dedupe key, so they're skipped entirely for v1. Other bank/account CSV formats
/// (Capital One, loan servicers) are explicitly out of scope.
/// </summary>
public class CsvImportService
{
    private const int DedupeHashVersion = 1;
    private const string PostedStatus = "Posted";

    /// <summary>
    /// Constructor.
    /// </summary>
    public CsvImportService(
        TransactionRepository transactionRepository,
        ImportBatchRepository importBatchRepository,
        ILogger<CsvImportService> logger)
    {
        _transactionRepository = transactionRepository;
        _importBatchRepository = importBatchRepository;
        _logger = logger;
    }

    private readonly TransactionRepository _transactionRepository;
    private readonly ImportBatchRepository _importBatchRepository;
    private readonly ILogger<CsvImportService> _logger;

    /// <summary>
    /// Imports the given CSV file into the given account, skipping rows that duplicate an
    /// already-imported transaction.
    /// </summary>
    public async Task<CsvImportResult> ImportAsync(int accountId, string filePath)
    {
        string[] lines;
        try
        {
            lines = await File.ReadAllLinesAsync(filePath);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to read CSV file at {FilePath}", filePath);
            return new CsvImportResult(0, 0, 0, 0, 0);
        }

        List<(DateTime Date, decimal Amount, string Description, string Status)> parsedRows = ParseRows(lines);
        int totalRows = parsedRows.Count;

        List<(DateTime Date, decimal Amount, string Description)> postedRows = parsedRows
            .Where(row => row.Status == PostedStatus)
            .Select(row => (row.Date, row.Amount, row.Description))
            .ToList();
        int skippedPendingRows = totalRows - postedRows.Count;

        HashSet<string> existingHashes = await _transactionRepository.GetExistingDedupeHashesAsync(accountId);
        var occurrenceCounts = new Dictionary<(DateTime, decimal, string), int>();

        int importedCount = 0;
        int duplicateCount = 0;

        foreach ((DateTime date, decimal amount, string description) in postedRows)
        {
            var key = (date, amount, description);
            occurrenceCounts.TryGetValue(key, out int occurrenceIndex);
            occurrenceCounts[key] = occurrenceIndex + 1;

            string dedupeHash = ComputeDedupeHash(date, amount, description, occurrenceIndex);

            if (existingHashes.Contains(dedupeHash))
            {
                duplicateCount++;
                continue;
            }

            var transaction = new Transaction
            {
                AccountId = accountId,
                Date = date,
                Amount = amount,
                Description = description,
                Source = TransactionSource.CsvImport,
                DedupeHash = dedupeHash,
                DedupeHashVersion = DedupeHashVersion
            };

            await _transactionRepository.AddAsync(transaction);

            // Guards against duplicate rows appearing more than once within this same file.
            existingHashes.Add(dedupeHash);
            importedCount++;
        }

        await _importBatchRepository.AddAsync(new ImportBatch
        {
            AccountId = accountId,
            ImportedAt = DateTime.UtcNow,
            FileName = Path.GetFileName(filePath),
            RowCount = totalRows,
            DuplicateCount = duplicateCount
        });

        return new CsvImportResult(totalRows, postedRows.Count, skippedPendingRows, importedCount, duplicateCount);
    }

    /// <summary>
    /// Parses the rows of a Format A CSV (header row excluded), tolerating and logging
    /// unparseable rows rather than failing the whole import. Exposed publicly so the parsing
    /// and Posted-only filtering can be unit tested without touching a database.
    /// </summary>
    public List<(DateTime Date, decimal Amount, string Description, string Status)> ParseRows(string[] lines)
    {
        var rows = new List<(DateTime, decimal, string, string)>();

        // Row 0 is the header (DATE,DESCRIPTION,AMOUNT,CHECK #,STATUS).
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string[] fields = SplitCsvLine(lines[i]);
            if (fields.Length < 5)
                continue;

            bool isDateValid = DateTime.TryParseExact(
                fields[0], "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date);
            bool isAmountValid = decimal.TryParse(fields[2], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount);

            if (!isDateValid || !isAmountValid)
            {
                _logger.LogWarning("Skipping unparseable CSV row: {Row}", lines[i]);
                continue;
            }

            rows.Add((date, amount, fields[1].Trim(), fields[4].Trim()));
        }

        return rows;
    }

    /// <summary>
    /// Computes the fallback dedup hash for a row: hash(date + amount + description) plus a
    /// same-day occurrence index, so two legitimately identical same-day transactions don't
    /// collapse into one. Exposed publicly for unit testing.
    /// </summary>
    public static string ComputeDedupeHash(DateTime date, decimal amount, string description, int occurrenceIndex)
    {
        string input = $"{date:yyyy-MM-dd}|{amount}|{description}|{occurrenceIndex}";
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Splits one CSV line into fields, honoring double-quoted fields (including escaped ""
    /// quotes within a field) rather than naively splitting on every comma.
    /// </summary>
    private static string[] SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }
}
