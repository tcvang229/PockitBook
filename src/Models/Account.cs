using System;

namespace PockitBook.Models;

/// <summary>
/// A financial account (checking, savings, credit card, etc.) that scheduled items,
/// transactions, and balance checkpoints belong to.
/// </summary>
public record Account
{
    /// <summary>
    /// The ID of this record.
    /// </summary>
    public int? Id { get; init; }

    /// <summary>
    /// The name of the account.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The kind of account.
    /// </summary>
    public required AccountType Type { get; init; }

    /// <summary>
    /// When this account was created.
    /// </summary>
    public required DateTime CreatedAt { get; init; }
}
