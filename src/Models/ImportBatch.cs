using System;

namespace PockitBook.Models;

/// <summary>
/// Tracks a single CSV import so it can be audited or undone as a whole, instead of imported
/// transactions being untraceable fire-and-forget inserts.
/// </summary>
public record ImportBatch
{
    /// <summary>
    /// The ID of this record.
    /// </summary>
    public int? Id { get; init; }

    /// <summary>
    /// The account this import was for.
    /// </summary>
    public required int AccountId { get; init; }

    /// <summary>
    /// When this import ran.
    /// </summary>
    public required DateTime ImportedAt { get; init; }

    /// <summary>
    /// The name of the imported file.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// How many rows were present in the file.
    /// </summary>
    public required int RowCount { get; init; }

    /// <summary>
    /// How many rows were skipped as duplicates of existing transactions.
    /// </summary>
    public required int DuplicateCount { get; init; }
}
