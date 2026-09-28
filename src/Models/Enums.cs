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
