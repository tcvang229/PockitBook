using System;
using LiveChartsCore.Defaults;

namespace PockitBook.Models;

/// <summary>
/// A DateTimePoint that also carries a human-readable label (the bill/income name for a
/// projected point, or the transaction description for an actual point) and the signed amount
/// that moved the balance to this point - lets the chart's tooltip show what a point actually
/// is and how much it changed, not just the running balance/date.
/// </summary>
public class LabeledDateTimePoint : DateTimePoint
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public LabeledDateTimePoint(DateTime dateTime, double? value, string label, decimal? delta = null) : base(dateTime, value)
    {
        Label = label;
        Delta = delta;
    }

    /// <summary>
    /// What this point represents, e.g. a ScheduledItem's Name or a Transaction's Description.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// The signed amount that moved the balance to this point (negative for a bill/debit,
    /// positive for income/credit) - null for a point that isn't an incremental movement (the
    /// starting-balance anchor, or a balance checkpoint/override, which is a hard reset rather
    /// than a delta).
    /// </summary>
    public decimal? Delta { get; }
}
