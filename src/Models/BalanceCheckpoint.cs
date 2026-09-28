using System;

namespace PockitBook.Models;

/// <summary>
/// A confirmed, known-true balance for an account as of a specific date. Replaces a single
/// mutable "starting balance" field with a timeline of confirmed points, so a manual override
/// or a reconciled import can correct drift without losing prior history.
/// </summary>
public record BalanceCheckpoint
{
    /// <summary>
    /// The ID of this record.
    /// </summary>
    public int? Id { get; init; }

    /// <summary>
    /// The account this checkpoint belongs to.
    /// </summary>
    public required int AccountId { get; init; }

    /// <summary>
    /// The date this balance is known-true as-of.
    /// </summary>
    public required DateTime Date { get; init; }

    /// <summary>
    /// The confirmed balance at that date.
    /// </summary>
    public required decimal Balance { get; init; }

    /// <summary>
    /// Where this checkpoint came from.
    /// </summary>
    public required TransactionSource Source { get; init; }

    /// <summary>
    /// An optional audit note, e.g. why a manual override was made.
    /// </summary>
    public string? Note { get; init; }
}
