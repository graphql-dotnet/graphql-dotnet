using System.Reflection;
using BenchmarkDotNet.Running;

namespace GraphQL.Benchmarks;

internal static class Program
{
    private static readonly Type _defaultBenchmark = typeof(ExecutionBenchmark);

    private static void Main(string[] args)
    {
        args = args.Select(x => x.ToLower()).ToArray();
        bool profile = args.FirstOrDefault() == "/p";
        int skip = 0;
        int profileCount = 0;
        if (profile)
        {
            skip = 1;
            if (args.Length >= 2 && int.TryParse(args[1], out profileCount))
            {
                skip = 2;
            }
            else
            {
                profileCount = 100;
            }
            args = args.Skip(skip).ToArray();
        }
        if (args.Length > 1)
        {
            Help(args);
            return;
        }
        Type benchmark = _defaultBenchmark; //default benchmark to run
        if (args.Length == 1)
        {
            benchmark = BenchmarkTypes().Where(x => x.Name.ToLower() == args[0]).SingleOrDefault();
            if (benchmark == null)
            {
                Help(args);
                return;
            }
        }
        if (profile)
        {
            RunProfilerPayload(benchmark, profileCount);
        }
        else
        {
            Console.WriteLine($"Starting benchmarks for {benchmark.Name}");
            Console.WriteLine();
            BenchmarkRunner.Run(benchmark);
        }
    }

    private static IEnumerable<Type> BenchmarkTypes()
    {
        return Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsClass && typeof(IBenchmark).IsAssignableFrom(x));
    }

    private static void Help(string[] args)
    {
        Console.WriteLine($"Invalid arguments: {string.Join(' ', args)}");
        Console.WriteLine(@"
Argument syntax: [/p [count]] [benchmarkTypeName]

        /p                  Runs profiler - if not specified, runs benchmarks
        count               Number of times to run execution before executing ReadLine
                               (defaults to 100)
        benchmarkTypeName   The name of the class to run; the class must inherit from IBenchmark
                               (defaults to " + _defaultBenchmark.Name + @")

Available benchmarks:
");
        foreach (var benchmark in BenchmarkTypes())
        {
            Console.WriteLine("        " + benchmark.Name);
        }
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
