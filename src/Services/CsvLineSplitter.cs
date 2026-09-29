using System.Collections.Generic;
using System.Text;

namespace PockitBook.Services;

/// <summary>
/// Splits one CSV line into fields, honoring double-quoted fields (including escaped ""
/// quotes within a field) rather than naively splitting on every comma. Shared by every
/// CSV import service so the quoting rules only need to be right in one place.
/// </summary>
internal static class CsvLineSplitter
{
    /// <summary>
    /// Splits a single CSV line into its fields.
    /// </summary>
    public static string[] Split(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }
}
