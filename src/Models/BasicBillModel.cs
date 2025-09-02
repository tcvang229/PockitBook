namespace PockitBook.Models;

/// <summary>
/// The model to be referenced for figuring out when bills are due for a given month.
/// </summary>
public record BasicBillModel
{
    /// <summary>
    /// The ID of this record.
    /// </summary>
    public int? Id { get; init; }

    /// <summary>
    /// The name of the bill.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The day of the month when the bill is usually due.
    /// </summary>
    public required int DueDayOfMonth { get; init; }

    /// <summary>
    /// The amount that is due for the bill. 
    /// </summary>
    public required float AmountDue { get; init; }
}