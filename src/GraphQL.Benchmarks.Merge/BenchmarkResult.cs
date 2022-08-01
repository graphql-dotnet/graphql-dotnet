using System.Text;

namespace GraphQL.Benchmarks.Merge;

internal sealed class BenchmarkResult
{
    public static BenchmarkResult Parse(string file)
    {
        var result = new BenchmarkResult();

        string[] lines = File.ReadAllLines(file);
        int i = 0;
        for (; i < lines.Length; ++i)
        {
            if (lines[i].StartsWith('|'))
                break;
            result.Header.Add(lines[i]);
        }

        result.Table = Table.Parse(lines, i);
        return result;
    }

    public List<string> Header = new();

    public Table Table { get; set; } = new Table();

    public BenchmarkResult RemoveColumns(string[] columnNames)
    {
        if (columnNames != null)
        {
            foreach (string name in columnNames)
                Table.Remove(name);
        }

        return this;
    }

    public void Save(string fileName)
    {
        if (File.Exists(fileName))
            File.Delete(fileName);

        var sb = new StringBuilder(2048);
        foreach (string line in Header)
            sb.AppendLine(line);

        sb.Append('|');
        foreach (var column in Table.Columns)
            sb.Append(column.Name).Append('|');

        sb.AppendLine().Append('|');
        for (int i = 0; i < Table.ColumnCount; ++i)
            sb.Append(Table.Columns[i].Alignment == Alignment.Right ? "--:" : "---").Append('|');
        sb.AppendLine();

        foreach (var line in Table.Rows)
        {
            sb.Append('|');
            foreach (string item in line)
                sb.Append(item).Append('|');
            sb.AppendLine();
        }

        File.WriteAllText(fileName, sb.ToString());
    }
}
