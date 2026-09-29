using System;
using System.Collections.Generic;
using System.Linq;
using PockitBook.Models;

namespace PockitBook.Services;

/// <summary>
/// Pure, DB-free expansion of ScheduledItem recurrence rules into projected balance points.
/// Pulled out of AccountProjectionViewModel so the recurrence math (Monthly/Weekly/Biweekly/
/// OneTime) can be unit tested without ReactiveUI/Avalonia.
/// </summary>
public class ProjectionCalculator
{
    /// <summary>
    /// Builds a projected balance line starting from `startingBalance` at `windowStart`, walking
    /// forward through `windowEnd`, applying every active scheduled item's occurrences in that
    /// range in date order.
    /// </summary>
    public List<LabeledDateTimePoint> BuildProjection(
        decimal startingBalance,
        DateTime windowStart,
        DateTime windowEnd,
        IEnumerable<ScheduledItem> scheduledItems)
    {
        List<(DateTime Date, decimal SignedAmount, string Label)> occurrences = scheduledItems
            .Where(item => item.IsActive)
            .SelectMany(item => ExpandOccurrences(item, windowStart, windowEnd))
            .OrderBy(occurrence => occurrence.Date)
            .ToList();

        List<LabeledDateTimePoint> points = new()
        {
            new LabeledDateTimePoint(windowStart, (double)startingBalance, "Starting balance")
        };

        decimal runningBalance = startingBalance;
        foreach ((DateTime date, decimal signedAmount, string label) in occurrences)
        {
            runningBalance += signedAmount;
            points.Add(new LabeledDateTimePoint(date, (double)runningBalance, label, signedAmount));
        }

        return points;
    }

    private static List<(DateTime Date, decimal SignedAmount, string Label)> ExpandOccurrences(
        ScheduledItem item,
        DateTime windowStart,
        DateTime windowEnd)
    {
        decimal signedAmount = item.Type == ScheduledItemType.Income ? item.ExpectedAmount : -item.ExpectedAmount;

        List<DateTime> candidateDates = item.Recurrence switch
        {
            RecurrenceType.OneTime => [item.AnchorDate],
            RecurrenceType.Weekly => ExpandInterval(item.AnchorDate, 7, windowEnd),
            RecurrenceType.Biweekly => ExpandInterval(item.AnchorDate, 14, windowEnd),
            RecurrenceType.Monthly => ExpandMonthly(item.AnchorDate, windowEnd),
            _ => []
        };

        var result = new List<(DateTime, decimal, string)>();
        foreach (DateTime rawDate in candidateDates)
        {
            // StartDate/EndDate bound the *intended* cadence (they're compared against rawDate)
            // rather than the adjusted date - e.g. a biweekly item whose StartDate equals its
            // AnchorDate must still include that very first occurrence even when the adjustment
            // shifts its displayed date a day or two earlier than StartDate.
            if (rawDate < item.StartDate)
                continue;
            if (item.EndDate is not null && rawDate > item.EndDate)
                continue;

            // Adjusted here, per-occurrence, rather than by shifting AnchorDate itself - the
            // interval/monthly math above always walks forward from the unshifted dates, so a
            // weekend shift never compounds or drifts the underlying cadence. windowStart/
            // windowEnd bound what's actually displayed, so they're checked against this real,
            // adjusted date instead.
            DateTime date = ApplyDateAdjustment(rawDate, item.DateAdjustment);
            if (date < windowStart || date > windowEnd)
                continue;

            result.Add((date, signedAmount, item.Name));
        }

        return result;
    }

    /// <summary>
    /// Applies a ScheduledItem's DateAdjustment rule to a single computed occurrence date.
    /// </summary>
    private static DateTime ApplyDateAdjustment(DateTime date, DateAdjustmentRule rule) =>
        rule switch
        {
            DateAdjustmentRule.NearestPriorBusinessDay => date.DayOfWeek switch
            {
                DayOfWeek.Saturday => date.AddDays(-1),
                DayOfWeek.Sunday => date.AddDays(-2),
                _ => date
            },
            _ => date
        };

    private static List<DateTime> ExpandInterval(DateTime anchorDate, int intervalDays, DateTime windowEnd)
    {
        var dates = new List<DateTime>();
        DateTime current = anchorDate;

        while (current <= windowEnd)
        {
            dates.Add(current);
            current = current.AddDays(intervalDays);
        }

        return dates;
    }

    private static List<DateTime> ExpandMonthly(DateTime anchorDate, DateTime windowEnd)
    {
        var dates = new List<DateTime>();
        int anchorDay = anchorDate.Day;
        DateTime cursor = new(anchorDate.Year, anchorDate.Month, 1);

        while (cursor <= windowEnd)
        {
            // Clamp to the last valid day of the month (e.g. an anchor day of 31 becomes the
            // 28th/30th in shorter months) instead of throwing.
            int day = Math.Min(anchorDay, DateTime.DaysInMonth(cursor.Year, cursor.Month));
            dates.Add(new DateTime(cursor.Year, cursor.Month, day));
            cursor = cursor.AddMonths(1);
        }

        return dates;
    }
}
