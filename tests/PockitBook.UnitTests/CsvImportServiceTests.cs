using Microsoft.Extensions.Logging;
using NSubstitute;
using PockitBook.Services;
using PockitBook.Repositories;

namespace PockitBook.UnitTests;

/// <summary>
/// Unit tests for CsvImportService's parsing, Posted-only filtering, and dedup-hash generation.
/// These don't touch a database - ImportAsync's DB-writing orchestration is covered by
/// integration tests instead.
/// </summary>
public class CsvImportServiceTests
{
    private static CsvImportService BuildSut()
    {
        var databaseLogger = Substitute.For<ILogger<SqliteDatabase>>();
        var database = Substitute.For<SqliteDatabase>("", databaseLogger, false);
        var transactionRepository = Substitute.For<TransactionRepository>(database, Substitute.For<ILogger<TransactionRepository>>());
        var importBatchRepository = Substitute.For<ImportBatchRepository>(database, Substitute.For<ILogger<ImportBatchRepository>>());
        var logger = Substitute.For<ILogger<CsvImportService>>();

        return new CsvImportService(transactionRepository, importBatchRepository, logger);
    }

    [Fact]
    public void ParseRows_ValidRows_ParsesDateAmountDescriptionStatus()
    {
        var sut = BuildSut();
        string[] lines =
        [
            "\"DATE\",\"DESCRIPTION\",\"AMOUNT\",\"CHECK #\",\"STATUS\"",
            "\"06/24/2026\",\"PURCHASE TARGET T-084\",\"-31.20\",\"\",\"Pending\"",
            "\"06/23/2026\",\"RECURRING PAYMENT Disney Plus\",\"-13.70\",\"\",\"Posted\""
        ];

        var rows = sut.ParseRows(lines);

        Assert.Equal(2, rows.Count);
        Assert.Equal(new DateTime(2026, 6, 24), rows[0].Date);
        Assert.Equal(-31.20m, rows[0].Amount);
        Assert.Equal("PURCHASE TARGET T-084", rows[0].Description);
        Assert.Equal("Pending", rows[0].Status);
        Assert.Equal("Posted", rows[1].Status);
    }

    [Fact]
    public void ParseRows_UnparseableRow_IsSkippedNotThrown()
    {
        var sut = BuildSut();
        string[] lines =
        [
            "\"DATE\",\"DESCRIPTION\",\"AMOUNT\",\"CHECK #\",\"STATUS\"",
            "\"not-a-date\",\"Some purchase\",\"-5.00\",\"\",\"Posted\"",
            "\"06/23/2026\",\"Valid row\",\"-13.70\",\"\",\"Posted\""
        ];

        var rows = sut.ParseRows(lines);

        Assert.Single(rows);
        Assert.Equal("Valid row", rows[0].Description);
    }

    [Fact]
    public void ParseRows_EmptyFile_ReturnsEmpty()
    {
        var sut = BuildSut();
        string[] lines = ["\"DATE\",\"DESCRIPTION\",\"AMOUNT\",\"CHECK #\",\"STATUS\""];

        var rows = sut.ParseRows(lines);

        Assert.Empty(rows);
    }

    [Fact]
    public void ComputeDedupeHash_SameInputs_ProducesSameHash()
    {
        var date = new DateTime(2026, 6, 24);

        string hash1 = CsvImportService.ComputeDedupeHash(date, -31.20m, "PURCHASE TARGET T-084", 0);
        string hash2 = CsvImportService.ComputeDedupeHash(date, -31.20m, "PURCHASE TARGET T-084", 0);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeDedupeHash_DifferentOccurrenceIndex_ProducesDifferentHash()
    {
        // Two legitimately identical same-day transactions (e.g. two $20 coffee charges) must
        // NOT collapse into the same dedup key - the occurrence index is what distinguishes them.
        var date = new DateTime(2026, 6, 24);

        string hash1 = CsvImportService.ComputeDedupeHash(date, -20.00m, "COFFEE SHOP", 0);
        string hash2 = CsvImportService.ComputeDedupeHash(date, -20.00m, "COFFEE SHOP", 1);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeDedupeHash_DifferentAmount_ProducesDifferentHash()
    {
        var date = new DateTime(2026, 6, 24);

        string hash1 = CsvImportService.ComputeDedupeHash(date, -20.00m, "COFFEE SHOP", 0);
        string hash2 = CsvImportService.ComputeDedupeHash(date, -25.00m, "COFFEE SHOP", 0);

        Assert.NotEqual(hash1, hash2);
    }
}
