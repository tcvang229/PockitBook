using System;

namespace PockitBook.Models;

/// <summary>
/// The model referenced for figuring out anticipated bills/income for a given account -
/// the recurring or one-time "recipe" that gets expanded into future projected occurrences.
/// </summary>
public record ScheduledItem
{
    /// <summary>
    /// The ID of this record.
    /// </summary>
    public int? Id { get; init; }

    /// <summary>
    /// The account this scheduled item belongs to.
    /// </summary>
    public required int AccountId { get; init; }

    /// <summary>
    /// The name of the bill or income.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Whether this is a bill (money out) or income (money in).
    /// </summary>
    public required ScheduledItemType Type { get; init; }

    /// <summary>
    /// The amount typically expected for this item.
    /// </summary>
    public required decimal ExpectedAmount { get; init; }

    /// <summary>
    /// How often this item recurs.
    /// </summary>
    public required RecurrenceType Recurrence { get; init; }

    /// <summary>
    /// The anchor date used for recurrence math - day-of-month for Monthly,
    /// a specific date for OneTime/Weekly/Biweekly cadence calculations.
    /// </summary>
    public required DateTime AnchorDate { get; init; }

    /// <summary>
    /// The date this item starts being projected from.
    /// </summary>
    public required DateTime StartDate { get; init; }

    /// <summary>
    /// The date this item stops being projected, if any (e.g. a loan payoff date).
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// A free-text category label.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Whether this item is currently active. Soft-disable instead of delete, to preserve history.
    /// </summary>
    public required bool IsActive { get; init; }
}
