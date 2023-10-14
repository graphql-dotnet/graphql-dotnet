using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;

namespace GraphQL.Benchmarks;

[MemoryDiagnoser]
public class TrimGraphQLTypesBenchmark
{
    private static readonly Regex _trimPattern = new("[\\[!\\]]", RegexOptions.Compiled);

    [Benchmark(Baseline = true)]
    public string Old() => _trimPattern.Replace(Name, string.Empty).Trim();

    [Benchmark]
    public string New() => Name.TrimGraphQLTypes();

    private static readonly char[] _chars = new[] { '[', ']', '!' };

    [Benchmark]
    public string Alt() => Name.Trim().Trim(_chars);

    [ParamsSource(nameof(Names))]
    public string Name { get; set; }

    public IEnumerable<string> Names => new[] { "", "Human", "Human!", "[Human]", "[Human]!", "[[Human!]!]!", "[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[]]]]]]]]]]]]]]]]]]]]]]]]]]]]]!!!!!!!!!!!!!!!!!!!!!!!!!!!" };
}
