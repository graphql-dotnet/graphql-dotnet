namespace GraphQL.Benchmarks.Merge;

internal sealed class Table
{
    public static Table Parse(string[] content, int from)
    {
        var table = new Table
        {
            Columns = content[from].Split('|').Select(i => new Column { Name = i.Trim() }).ToList()
        };
        table.Columns.RemoveAt(0);
        table.Columns.RemoveAt(table.ColumnCount - 1);

        from += 2; // skip |--------|--------|

        for (; from < content.Length; ++from)
        {
            if (!content[from].StartsWith('|'))
                break;

            var data = content[from].Split('|').Select(i => i.Trim()).ToList();
            data.RemoveAt(0);
            data.RemoveAt(data.Count - 1);
            table.Rows.Add(data);
        }

        return table;
    }

    public List<Column> Columns { get; set; } = new List<Column>();

    public int ColumnCount => Columns.Count;

    public List<List<string>> Rows { get; set; } = new List<List<string>>();

    public int RowCount => Rows.Count;

    public void Remove(string columnName)
    {
        int index = Columns.FindIndex(c => c.Name == columnName);
        if (index == -1)
            return;

        Columns.RemoveAt(index);

        foreach (var line in Rows)
            line.RemoveAt(index);
    }
}
