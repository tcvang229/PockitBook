namespace PockitBook.Models;

/// <summary>
/// The model to be referenced for figuring out when bills are due for a given month.
/// </summary>
public class BasicBillModel
{
    /// <summary>
    /// The name of the bill.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The day of the month when the bill is usually due.
    /// </summary>
    public int DueDayOfMonth { get; set; } = 1;
}