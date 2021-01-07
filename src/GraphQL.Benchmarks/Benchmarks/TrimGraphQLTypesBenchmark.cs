using System.Collections.Generic;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;

namespace GraphQL.Benchmarks
{
    [MemoryDiagnoser]
    public class TrimGraphQLTypesBenchmark
    {
        private static readonly Regex _trimPattern = new Regex("[\\[!\\]]", RegexOptions.Compiled);

        [Benchmark(Baseline = true)]
        public string TrimGraphQLTypesOld() => _trimPattern.Replace(Name, string.Empty).Trim();

        [Benchmark]
        public string TrimGraphQLTypesNew() => Name.TrimGraphQLTypes();

        [ParamsSource(nameof(Names))]
        public string Name { get; set; }

        public IEnumerable<string> Names => new[] { "", "Human", "Human!", "[Human]", "[Human]!", "[[Human!]!]!", "[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[]]]]]]]]]]]]]]]]]]]]]]]]]]]]]!!!!!!!!!!!!!!!!!!!!!!!!!!!" };
    }
}
