using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace GraphQL.Benchmarks.Merge
{
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

            var before = BenchmarkResult.Parse(Options.Before).RemoveColumns("Error", "StdDev", "Gen 0", "Gen 1", "Gen 2");
            var after = BenchmarkResult.Parse(Options.After).RemoveColumns("Error", "StdDev", "Gen 0", "Gen 1", "Gen 2");
            var result = Merge(before, after, "Mean", "Allocated");
            result.Save(Options.Result);

            return 0;
        }

        private static BenchmarkResult Merge(BenchmarkResult before, BenchmarkResult after, params string[] diffColumns)
        {
            var result = new BenchmarkResult
            {
                Header = before.Header
            };

            foreach (string column in before.Columns.Names)
            {
                if (diffColumns.Contains(column))
                {
                    result.Columns.Names.Add(column + " [base]");
                    result.Columns.Names.Add(column + " [current]");
                    result.Columns.Names.Add(column + " [% of base]");
                }
                else
                {
                    result.Columns.Names.Add(column);
                }
            }

            for (int line = 0; line < before.Columns.Data.Count; ++line)
            {
                var resultList = new List<string>();

                for (int column = 0; column < before.Columns.Names.Count; ++column)
                {
                    string name = before.Columns.Names[column];
                    string beforeValue = before.Columns.Data[line][column];
                    string afterValue = after.Columns.Data[line][column];

                    if (diffColumns.Contains(name))
                    {
                        resultList.Add(beforeValue);
                        resultList.Add(afterValue);
                        resultList.Add(GetDifference(beforeValue, afterValue));
                    }
                    else
                    {
                        if (beforeValue != afterValue)
                            throw new InvalidOperationException($"Different values: {beforeValue} | {afterValue}");

                        resultList.Add(beforeValue);
                    }
                }

                result.Columns.Data.Add(resultList);
            }

            return result;
        }

        private static string GetDifference(string before, string after)
        {
            double valueBefore = GetValue(before);
            double valueAfter = GetValue(after);

            int percent = (int)(valueAfter * 100 / valueBefore);

            string b1 = before.StartsWith('*') ? "<b>" : "";
            string b2 = before.StartsWith('*') ? "</b>" : "";

            if (percent < 100)
                return $"<span style=\"color:green\">{b1}{percent} [better]{b2}</span>";
            if (percent > 100)
                return $"<span style=\"color:red\">{b1}{percent} [worse]{b2}</span>";
            else
                return $"{b1}{percent}{b2}";
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
}
