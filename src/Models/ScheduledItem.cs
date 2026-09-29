using System;

namespace PockitBook.Models;

/// <summary>
/// The model referenced for figuring out anticipated bills/income for a given account -
/// the recurring or one-time "recipe" that gets expanded into future projected occurrences.
///
/// Properties are mutable (not init-only) so this can be edited in place from the Bill Details
/// grid - see BillDetailsView's DataGrid, which two-way binds directly to these setters.
/// </summary>
public record ScheduledItem
{
    /// <summary>
    /// The ID of this record.
    /// </summary>
    public int? Id { get; set; }

    /// <summary>
    /// The account this scheduled item belongs to.
    /// </summary>
    public required int AccountId { get; set; }

    /// <summary>
    /// The name of the bill or income.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Whether this is a bill (money out) or income (money in).
    /// </summary>
    public required ScheduledItemType Type { get; set; }

    /// <summary>
    /// The amount typically expected for this item. Always a non-negative magnitude - the
    /// Bill/Income sign is applied by consumers (e.g. ProjectionCalculator), never stored here.
    /// </summary>
    public required decimal ExpectedAmount
    {
        get => _expectedAmount;
        set => _expectedAmount = Math.Abs(value);
    }

    /// <summary>
    /// How often this item recurs.
    /// </summary>
    public required RecurrenceType Recurrence { get; set; }

    /// <summary>
    /// The anchor date used for recurrence math - day-of-month for Monthly,
    /// a specific date for OneTime/Weekly/Biweekly cadence calculations.
    /// </summary>
    public required DateTime AnchorDate { get; set; }

    /// <summary>
    /// The day-of-month component of AnchorDate, as a directly settable value for the Bill
    /// Details grid's "Due Day" column - AnchorDate.Day itself isn't settable since DateTime is
    /// immutable. Setting this rebuilds AnchorDate with the same year/month, clamped to the
    /// number of days that month actually has (e.g. 31 in February becomes the 28th/29th).
    /// Only meaningful for Monthly recurrence, which is all the Bill Details page creates today.
    /// </summary>
    public int DueDay
    {
        get => AnchorDate.Day;
        set
        {
            int clampedDay = Math.Clamp(value, 1, DateTime.DaysInMonth(AnchorDate.Year, AnchorDate.Month));
            AnchorDate = new DateTime(AnchorDate.Year, AnchorDate.Month, clampedDay);
        }
    }

    /// <summary>
    /// The date this item starts being projected from.
    /// </summary>
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// The date this item stops being projected, if any (e.g. a loan payoff date).
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// A free-text category label.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Whether this item is currently active. Soft-disable instead of delete, to preserve history.
    /// </summary>
    public required bool IsActive { get; set; }

    /// <summary>
    /// How a projected occurrence date should be adjusted when it lands on a non-business day
    /// (e.g. a weekend paycheck date shifted to the preceding Friday). Applied only to the
    /// *displayed*/projected date - ProjectionCalculator's underlying cadence math always keeps
    /// counting from the unshifted dates, so a shift never compounds or drifts the recurrence.
    /// Not required (defaults to None) so existing construction sites don't need updating.
    /// </summary>
    public DateAdjustmentRule DateAdjustment { get; set; } = DateAdjustmentRule.None;

    private decimal _expectedAmount;
}
