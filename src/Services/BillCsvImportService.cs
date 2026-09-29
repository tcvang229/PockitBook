using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PockitBook.Models;
using PockitBook.Repositories;

namespace PockitBook.Services;

/// <summary>
/// The outcome of a bill CSV import, used to show a summary to the user.
/// </summary>
public record BillCsvImportResult(int TotalRows, int ImportedRows, int SkippedRows);

/// <summary>
/// Imports bills/income from a CSV file into scheduled_items. This *extends* whatever is
/// already there rather than replacing it - existing rows are left untouched, and the Bill
/// Details grid's inline editing/delete is the way to clean up anything the import got wrong.
///
/// Expected format (header row required): Name,DueDay,Amount,Type
/// - Name: the bill/income name.
/// - DueDay: day of month, 1-31 (clamped to the current month's actual length, e.g. 31 in
///   February becomes the 28th/29th - same clamping the manual Add Bill form uses).
/// - Amount: a non-negative numeric magnitude (the Bill/Income sign comes from Type, not stored
///   here - same convention as ScheduledItem.ExpectedAmount elsewhere).
/// - Type: "Bill" or "Income", case-insensitive; optional, defaults to "Bill" if blank/omitted.
/// Every imported row becomes a Monthly-recurring, active ScheduledItem starting today - other
/// recurrences aren't exposed by this page yet (see the README's "Known issues" note).
/// </summary>
public class BillCsvImportService
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public BillCsvImportService(ScheduledItemRepository scheduledItemRepository, ILogger<BillCsvImportService> logger)
    {
        _scheduledItemRepository = scheduledItemRepository;
        _logger = logger;
    }

    private readonly ScheduledItemRepository _scheduledItemRepository;
    private readonly ILogger<BillCsvImportService> _logger;

    /// <summary>
    /// Imports the given CSV file's bills/income into the given account, adding to whatever
    /// scheduled items already exist.
    /// </summary>
    public async Task<BillCsvImportResult> ImportAsync(int accountId, string filePath)
    {
        string[] lines;
        try
        {
            lines = await File.ReadAllLinesAsync(filePath);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to read CSV file at {FilePath}", filePath);
            return new BillCsvImportResult(0, 0, 0);
        }

        List<ScheduledItem> parsedItems = ParseRows(lines, accountId);

        foreach (ScheduledItem item in parsedItems)
            await _scheduledItemRepository.AddAsync(item);

        int totalRows = Math.Max(lines.Length - 1, 0);
        return new BillCsvImportResult(totalRows, parsedItems.Count, totalRows - parsedItems.Count);
    }

    /// <summary>
    /// Parses the rows of a bill CSV (header row excluded) into ScheduledItems, tolerating and
    /// logging unparseable rows rather than failing the whole import. Exposed publicly so
    /// parsing can be unit tested without touching a database.
    /// </summary>
    public List<ScheduledItem> ParseRows(string[] lines, int accountId)
    {
        var items = new List<ScheduledItem>();
        DateTime today = DateTime.Today;

        // Row 0 is the header (Name,DueDay,Amount,Type).
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string[] fields = CsvLineSplitter.Split(lines[i]);
            if (fields.Length < 3)
            {
                _logger.LogWarning("Skipping unparseable bill CSV row: {Row}", lines[i]);
                continue;
            }

            string name = fields[0].Trim();
            bool isDueDayValid = int.TryParse(fields[1].Trim(), out int dueDay);
            bool isAmountValid = decimal.TryParse(fields[2].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount);

            if (string.IsNullOrWhiteSpace(name) || !isDueDayValid || dueDay < 1 || dueDay > 31 || !isAmountValid)
            {
                _logger.LogWarning("Skipping unparseable bill CSV row: {Row}", lines[i]);
                continue;
            }

            string typeField = fields.Length > 3 ? fields[3].Trim() : string.Empty;
            ScheduledItemType type = Enum.TryParse(typeField, ignoreCase: true, out ScheduledItemType parsedType)
                ? parsedType
                : ScheduledItemType.Bill;

            int clampedDay = Math.Min(dueDay, DateTime.DaysInMonth(today.Year, today.Month));
            DateTime anchorDate = new(today.Year, today.Month, clampedDay);

            items.Add(new ScheduledItem
            {
                AccountId = accountId,
                Name = name,
                Type = type,
                ExpectedAmount = amount,
                Recurrence = RecurrenceType.Monthly,
                AnchorDate = anchorDate,
                StartDate = today,
                EndDate = null,
                IsActive = true
            });
        }

        return items;
    }
}
