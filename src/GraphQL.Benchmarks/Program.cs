using BenchmarkDotNet.Running;
using System;

namespace GraphQL.Benchmarks
{
    public class Program
    {
        public static void Main()
        {
            var summary = BenchmarkRunner.Run<ExecutionBenchmark>();
            //if (summary.HasCriticalValidationErrors)
                Console.ReadLine();
        }
    }
}
