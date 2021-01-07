using System;
using System.Threading;
using BenchmarkDotNet.Running;

namespace GraphQL.Benchmarks
{
    internal static class Program
    {
        // Call without args for BenchmarkDotNet
        // Call with some arbitrary args for any memory profiler
        private static void Main(string[] args) => Run<ValidationBenchmark>(args);

        private static void Run<TBenchmark>(string[] args)
            where TBenchmark : IBenchmark, new()
        {
            if (args.Length == 0)
                _ = BenchmarkRunner.Run<TBenchmark>();
            else
                RunMemoryProfilerPayload<TBenchmark>();
        }

        private static void RunMemoryProfilerPayload<TBenchmark>()
            where TBenchmark : IBenchmark, new()
        {
            var bench = new TBenchmark();
            bench.GlobalSetup();

            int count = 0;
            while (true)
            {
                bench.Run();

                Thread.Sleep(10);

                if (++count % 100 == 0)
                    Console.ReadLine();
            }
        }
    }
}
