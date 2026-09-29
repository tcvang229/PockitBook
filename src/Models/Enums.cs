namespace PockitBook.Models;

/// <summary>
/// The kind of financial account.
/// </summary>
public enum AccountType
{
    Checking,
    Savings,
    Credit
}

/// <summary>
/// Whether a scheduled item represents money going out or coming in.
/// </summary>
public enum ScheduledItemType
{
    Bill,
    Income
}

/// <summary>
/// How often a scheduled item recurs.
/// </summary>
public enum RecurrenceType
{
    OneTime,
    Weekly,
    Biweekly,
    Monthly
}

/// <summary>
/// Where a record originated from.
/// </summary>
public enum TransactionSource
{
    Manual,
    CsvImport,
    BankSync
}

/// <summary>
/// How a projected ScheduledItem occurrence date should be adjusted when the raw recurrence
/// math lands it on a non-business day.
/// </summary>
public enum DateAdjustmentRule
{
    /// <summary>
    /// Use the occurrence date exactly as computed - no adjustment.
    /// </summary>
    None,

    /// <summary>
    /// If the occurrence date is a Saturday or Sunday, shift it back to the preceding Friday -
    /// matching how real payroll systems handle a payday that falls on a weekend. Bank-holiday
    /// shifting is intentionally not covered (would need a holiday calendar - see the refactor
    /// design doc's cut list).
    /// </summary>
    NearestPriorBusinessDay
}
