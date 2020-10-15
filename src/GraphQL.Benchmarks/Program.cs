using System;
using System.Threading;
using BenchmarkDotNet.Running;

namespace GraphQL.Benchmarks
{
    internal static class Program
    {
        // Call without args for BenchmarkDotNet
        // Call with some arbitrary args for any memory profiler
        private static void Main(string[] args)
        {
            if (args.Length == 0)
                BenchmarkRunner.Run<SerializationBenchmark>();
            else
                RunMemoryProfilerPayload();
        }

        private static void RunMemoryProfilerPayload()
        {
            var bench = new SerializationBenchmark();
            bench.GlobalSetup();

            int count = 0;
            while (true)
            {
                bench.SystemTextJson().Wait();

                Thread.Sleep(10);

                ++count;
                if (count == 100)
                    break;
            }

            Console.WriteLine("========== END ==========");
            Console.ReadLine();
        }
    }
}
