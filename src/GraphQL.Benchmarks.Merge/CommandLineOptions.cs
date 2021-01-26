using CommandLine;

namespace GraphQL.Benchmarks.Merge
{
    internal sealed class CommandLineOptions
    {
        [Option("before", Required = false, Default = "before.md", HelpText = "BenchmarkDotNet result file before your changes")]
        public string Before { get; set; }

        [Option("after", Required = false, Default = "after.md", HelpText = "BenchmarkDotNet result file after your changes")]
        public string After { get; set; }

        [Option("result", Required = false, Default = "diff.md", HelpText = "BenchmarkDotNet resulting diff file")]
        public string Result { get; set; }
    }
}
