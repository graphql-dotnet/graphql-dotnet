using CommandLine;

namespace GraphQL.Benchmarks.Merge;

internal static class Program
{
    internal static int Main(string[] args) =>
     Parser.Default.ParseArguments<CommandLineOptions>(args).MapResult(
           (CommandLineOptions opt) => new Merger(opt).Merge(),
           errors => -1);
}
