using System.Reflection;
using BenchmarkDotNet.Running;

// USAGE
// ─────────────────────────────────────────────────────────────────────────────
// Full BenchmarkDotNet run (all benchmarks):
//   dotnet run -c Release
//
// Run a specific benchmark class (case-insensitive class name):
//   dotnet run -c Release -- --filter *ValidationBenchmark*
//
// Run a specific benchmark method:
//   dotnet run -c Release -- --filter *ManyInlineFragments*
//
// Run in-process:
//   dotnet run -c Release -- --inProcess
//
// Any other BenchmarkDotNet CLI flags (--job, --exporters, etc.) are forwarded
// directly to BenchmarkSwitcher.
//
// PROFILER MODE  (/p)
// ─────────────────────────────────────────────────────────────────────────────
// Runs IBenchmark.RunProfiler() in a tight loop so an external profiler
// (e.g. dotTrace, PerfView) can attach.  Press Enter between batches.
//
//   /p                  Run default benchmark (ExecutionBenchmark), 100 iters/batch
//   /p <count>          Run default benchmark, <count> iters/batch
//   /p <benchmarkName>  Run named benchmark class, 100 iters/batch
//   /p <count> <name>   Run named benchmark class, <count> iters/batch
//
// Examples:
//   dotnet run -c Release -- /p validationbenchmark
//   dotnet run -c Release -- /p 50 validationbenchmark
// ─────────────────────────────────────────────────────────────────────────────

namespace GraphQL.Benchmarks;

internal static class Program
{
    private static readonly Type _defaultBenchmark = typeof(ExecutionBenchmark);

    private static void Main(string[] args)
    {
        var normalizedArgs = args.Select(x => x.ToLower()).ToArray();
        bool profile = normalizedArgs.FirstOrDefault() == "/p";

        if (profile)
        {
            int skip = 1;
            int profileCount = 100;
            if (normalizedArgs.Length >= 2 && int.TryParse(normalizedArgs[1], out int parsed))
            {
                profileCount = parsed;
                skip = 2;
            }
            normalizedArgs = normalizedArgs.Skip(skip).ToArray();

            Type benchmark = _defaultBenchmark;
            if (normalizedArgs.Length == 1)
            {
                benchmark = BenchmarkTypes().SingleOrDefault(x => x.Name.ToLower() == normalizedArgs[0])
                    ?? _defaultBenchmark;
            }

            RunProfilerPayload(benchmark, profileCount);
        }
        else
        {
            // Delegate everything to BenchmarkSwitcher so that all BenchmarkDotNet
            // command-line options work (--filter, --inProcess, --job, etc.).
            BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run(args);
        }
    }

    private static IEnumerable<Type> BenchmarkTypes()
    {
        return Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsClass && typeof(IBenchmark).IsAssignableFrom(x));
    }

    private static void RunProfilerPayload(Type benchmarkType, int count)
    {
        var m = typeof(Program).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(x => x.Name == nameof(RunProfilerPayload) && x.ContainsGenericParameters).Single();
        m.MakeGenericMethod(benchmarkType).Invoke(null, new object[] { count });
    }

    private static void RunProfilerPayload<TBenchmark>(int count)
        where TBenchmark : IBenchmark, new()
    {
        var bench = new TBenchmark();
        bench.GlobalSetup();

        int index = 0;
        while (true)
        {
            bench.RunProfiler();

            Thread.Sleep(10);

            if (++index % count == 0)
            {
                Console.WriteLine($"{count} iterations completed, press enter");
                Console.ReadLine();
            }
        }
    }
}
