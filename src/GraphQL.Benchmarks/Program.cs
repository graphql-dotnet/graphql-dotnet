using System;
using System.Threading;
using BenchmarkDotNet.Running;

namespace GraphQL.Benchmarks
{
    internal static class Program
    {
        // Call without args for BenchmarkDotNet
        // Call with some arbitrary args for any profiler
        private static void Main(string[] args) => Run<ExecutionBenchmark>(args);

        private static void Run<TBenchmark>(string[] args)
            where TBenchmark : IBenchmark, new()
        {
            if (args.Length == 0)
                _ = BenchmarkRunner.Run<TBenchmark>();
            else
                RunProfilerPayload<TBenchmark>(100);
        }

        private static void RunProfilerPayload<TBenchmark>(int count)
            where TBenchmark : IBenchmark, new()
        {
            var bench = new TBenchmark();
            bench.GlobalSetup();

            int index = 0;
            while (true)
            {
                bench.Run();

                Thread.Sleep(10);

                if (++index % count == 0)
                    Console.ReadLine();
            }
        }
    }
}
