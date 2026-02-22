using System.Text;

namespace LearningFlashCards.Core.Application.Import;

public sealed class CsvCardImportParser
{
    public CsvCardImportResult Parse(string content)
    {
        var cards = new List<ImportedCardRow>();
        var invalidRows = new List<int>();

        var rows = ParseCsvContent(content);
        var headerChecked = false;
        var rowNumber = 0;

        foreach (var fields in rows)
        {
            if (fields.Count == 0 || fields.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            if (!headerChecked)
            {
                headerChecked = true;
                if (IsHeaderRow(fields))
                {
                    continue;
                }
            }

            rowNumber++;

            var front = fields.ElementAtOrDefault(0)?.Trim() ?? string.Empty;
            var back = fields.ElementAtOrDefault(1)?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(front) || string.IsNullOrWhiteSpace(back))
            {
                invalidRows.Add(rowNumber);
                continue;
            }

            cards.Add(new ImportedCardRow(rowNumber, front, back));
        }

        return new CsvCardImportResult(cards, invalidRows);
    }

    private static List<List<string>> ParseCsvContent(string content)
    {
        var rows = new List<List<string>>();
        var fields = new List<string>();
        var buffer = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < content.Length; i++)
        {
            var ch = content[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < content.Length && content[i + 1] == '"')
                {
                    buffer.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (!inQuotes)
            {
                if (ch == ',')
                {
                    fields.Add(buffer.ToString());
                    buffer.Clear();
                    continue;
                }

                if (ch == '\r' || ch == '\n')
                {
                    if (ch == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                    {
                        i++;
                    }

                    fields.Add(buffer.ToString());
                    buffer.Clear();
                    rows.Add(fields);
                    fields = new List<string>();
                    continue;
                }
            }

            buffer.Append(ch);
        }

        if (buffer.Length > 0 || fields.Count > 0)
        {
            fields.Add(buffer.ToString());
            rows.Add(fields);
        }

        return rows;
    }

    private static bool IsHeaderRow(IReadOnlyList<string> fields)
    {
        if (fields.Count < 2)
        {
            return false;
        }

        var front = TrimHeaderField(fields[0]);
        var back = TrimHeaderField(fields[1]);

        return front.Equals("front", StringComparison.OrdinalIgnoreCase)
            && back.Equals("back", StringComparison.OrdinalIgnoreCase);
    }

    private static string TrimHeaderField(string value)
    {
        return value.Trim().TrimStart('\uFEFF');
    }
}

public sealed record CsvCardImportResult(
    IReadOnlyList<ImportedCardRow> Cards,
    IReadOnlyList<int> InvalidRowNumbers);

public sealed record ImportedCardRow(int RowNumber, string Front, string Back);
