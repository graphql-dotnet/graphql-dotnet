using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace GraphQL.Benchmarks
{
    [MemoryDiagnoser]
    public class ToCamelCase
    {
        public static string ToCamelCaseOld1(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return string.Empty;
            }

            var newFirstLetter = char.ToLowerInvariant(s[0]);
            if (newFirstLetter == s[0])
                return s;

            return newFirstLetter + s.Substring(1);
        }

        [Benchmark(Baseline = true)]
        public string ToCamelCaseOld() => ToCamelCaseOld1(Name);

        [Benchmark]
        public string ToCamelCaseNew() => StringExtensions.ToCamelCase(Name);

        [ParamsSource(nameof(Names))]
        public string Name { get; set; }

        public IEnumerable<string> Names => new[] { "", "short", "Short", "looooooooooooooooooooooooooooooooooooooooooooong", "Looooooooooooooooooooooooooooooooooooooooooooong" };
    }
}
