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
        foreach (DateTime date in candidateDates)
        {
            if (date < windowStart || date > windowEnd)
                continue;
            if (date < item.StartDate)
                continue;
            if (item.EndDate is not null && date > item.EndDate)
                continue;

            result.Add((date, signedAmount, item.Name));
        }

        return result;
    }

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
