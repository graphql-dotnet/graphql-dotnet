using System.Globalization;

namespace GraphQL.Benchmarks.Merge;

internal sealed class Merger
{
    public CommandLineOptions Options { get; }

    public Merger(CommandLineOptions options)
    {
        Options = options;
    }

    public int Merge()
    {
        if (!File.Exists(Options.Before))
        {
            Console.Error.WriteLine($"{Options.Before} file doesn't exist");
            return -1;
        }

        if (!File.Exists(Options.After))
        {
            Console.Error.WriteLine($"{Options.After} file doesn't exist");
            return -1;
        }

        var excludeColumns = Options.ExcludeColumns?.Split(';');
        var compareColumns = Options.CompareColumns?.Split(';');

        var before = BenchmarkResult.Parse(Options.Before).RemoveColumns(excludeColumns);
        var after = BenchmarkResult.Parse(Options.After).RemoveColumns(excludeColumns);
        var result = Merge(before, after, compareColumns);
        result.Save(Options.Result);

        return 0;
    }

    private static BenchmarkResult Merge(BenchmarkResult before, BenchmarkResult after, string[] compareColumns)
    {
        if (!before.Header.SequenceEqual(after.Header))
        {
            throw new InvalidOperationException($@"Differences were found in the benchmark execution environments.
BEFORE:
{string.Join(Environment.NewLine, before.Header)}

AFTER:
{string.Join(Environment.NewLine, after.Header)}");
        }

        var result = new BenchmarkResult
        {
            Header = before.Header
        };

        foreach (var column in before.Table.Columns)
        {
            if (compareColumns.Contains(column.Name))
            {
                result.Table.Columns.Add(new Column { Name = column.Name + " [base]", Alignment = Alignment.Right });
                result.Table.Columns.Add(new Column { Name = column.Name + " [current]", Alignment = Alignment.Right });
                result.Table.Columns.Add(new Column { Name = column.Name + " [% of base]", Alignment = Alignment.Right });
            }
            else
            {
                result.Table.Columns.Add(new Column { Name = column.Name, Alignment = Alignment.Left });
            }
        }

        for (int row = 0; row < before.Table.RowCount; ++row)
        {
            var resultList = new List<string>();

            for (int column = 0; column < before.Table.ColumnCount; ++column)
            {
                string name = before.Table.Columns[column].Name;
                string beforeValue = before.Table.Rows[row][column];
                string afterValue = after.Table.Rows[row][column];

                if (compareColumns.Contains(name))
                {
                    resultList.Add(beforeValue);
                    resultList.Add(afterValue);
                    resultList.Add(GetDifference(beforeValue, afterValue));
                }
                else
                {
                    if (beforeValue != afterValue)
                        throw new InvalidOperationException($"Different values in column '{name}': {beforeValue} | {afterValue}. Add this column either in --exclude or --compare command line switch.");

                    resultList.Add(beforeValue);
                }
            }

            result.Table.Rows.Add(resultList);
        }

        return result;
    }

    private static string GetDifference(string before, string after)
    {
        if (before == "" && after == "")
            return "";

        int? percent = before == "-" || after == "-"
            ? null
            : (int?)(GetValue(after) * 100 / GetValue(before));

        string b1 = before.StartsWith('*') ? "<b>" : "";
        string b2 = before.StartsWith('*') ? "</b>" : "";

        if (percent < 100)
            return $"<span style=\"color:green\">{b1}{percent}{b2}</span>";
        if (percent > 100)
            return $"<span style=\"color:red\">{b1}{percent}{b2}</span>";
        else if (percent == 100)
            return $"{b1}{percent}{b2}";
        else
            return "-";
    }

    private static double GetValue(string value)
    {
        value = value.Trim('*');

        for (int i = value.Length - 1; i >= 0; --i)
        {
            if (char.IsDigit(value[i]))
            {
                value = value.Substring(0, i + 1).Replace(",", "");
                return double.Parse(value, CultureInfo.InvariantCulture);
            }
        }

        throw new InvalidOperationException($"No value: {value}");
    }
}
