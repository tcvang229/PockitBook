using System;

namespace PockitBook.Models;

/// <summary>
/// A record of actual money movement - the "concrete track record" as opposed to a
/// ScheduledItem's anticipated/projected one.
/// </summary>
public record Transaction
{
    /// <summary>
    /// The ID of this record.
    /// </summary>
    public int? Id { get; init; }

    /// <summary>
    /// The account this transaction belongs to.
    /// </summary>
    public required int AccountId { get; init; }

    /// <summary>
    /// The date the transaction occurred.
    /// </summary>
    public required DateTime Date { get; init; }

    /// <summary>
    /// The signed amount - positive for credits, negative for debits.
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// The description/payee/memo for this transaction.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// A free-text category label.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// The scheduled item this transaction fulfilled, if manually linked.
    /// </summary>
    public int? ScheduledItemId { get; init; }

    /// <summary>
    /// Where this transaction record came from.
    /// </summary>
    public required TransactionSource Source { get; init; }

    /// <summary>
    /// The authoritative transaction id provided by a bank/API source, if any.
    /// Never populated from a computed hash - see DedupeHash.
    /// </summary>
    public string? ExternalId { get; init; }

    /// <summary>
    /// Our own computed fallback dedup key, used only when the source doesn't provide
    /// a stable ExternalId. Kept separate from ExternalId so the hash algorithm can change
    /// later without ever touching authoritative bank-provided ids.
    /// </summary>
    public string? DedupeHash { get; init; }

    /// <summary>
    /// Which version of the dedup hash algorithm produced DedupeHash.
    /// </summary>
    public int? DedupeHashVersion { get; init; }

    /// <summary>
    /// The import batch this transaction came from, if it was imported from a file.
    /// </summary>
    public int? ImportBatchId { get; init; }
}
