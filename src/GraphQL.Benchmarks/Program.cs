using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using System.Threading;

namespace GraphQL.Benchmarks
{
    internal static class Program
    {
        private static void Main()
        {
            //var config = new ManualConfig().With(new ConsoleLogger()).With(ConfigOptions.DisableOptimizationsValidator | ConfigOptions.KeepBenchmarkFiles);
            BenchmarkRunner.Run<IntrospectionBenchmark>(/*config*/);
        }

        private static void Main1()
        {
            var bench = new IntrospectionBenchmark();
            bench.GlobalSetup();
            while (true)
            {
                bench.Introspection();
                Thread.Sleep(1000);
            }
        }
    }
}
