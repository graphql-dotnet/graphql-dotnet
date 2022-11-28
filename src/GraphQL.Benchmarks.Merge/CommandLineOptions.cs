using CommandLine;

namespace GraphQL.Benchmarks.Merge;

internal sealed class CommandLineOptions
{
    [Option("before", Required = false, Default = "before.md", HelpText = "BenchmarkDotNet result file before your changes")]
    public string Before { get; set; }

    [Option("after", Required = false, Default = "after.md", HelpText = "BenchmarkDotNet result file after your changes")]
    public string After { get; set; }

    [Option("result", Required = false, Default = "diff.md", HelpText = "BenchmarkDotNet resulting diff file")]
    public string Result { get; set; }

    [Option("exclude", Required = false, Default = "Median;Error;StdDev;Gen 0;Gen 1;Gen 2", HelpText = "Columns to exclude from the resulting diff file")]
    public string ExcludeColumns { get; set; }

    [Option("compare", Required = false, Default = "Mean;Allocated", HelpText = "Columns to compare in the resulting diff file")]
    public string CompareColumns { get; set; }
}
