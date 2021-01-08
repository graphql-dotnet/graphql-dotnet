using System;
using BenchmarkDotNet.Attributes;
using GraphQL.StarWars;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Benchmarks
{
    [MemoryDiagnoser]
    //[RPlotExporter, CsvMeasurementsExporter]
    public class ExecutionBenchmark : IBenchmark
    {
        private IServiceProvider _provider;
        private ISchema _schema;
        private DocumentExecuter _executer;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var services = new ServiceCollection();

            services.AddSingleton<StarWarsData>();
            services.AddSingleton<StarWarsQuery>();
            services.AddSingleton<StarWarsMutation>();
            services.AddSingleton<HumanType>();
            services.AddSingleton<HumanInputType>();
            services.AddSingleton<DroidType>();
            services.AddSingleton<CharacterInterface>();
            services.AddSingleton<EpisodeEnum>();
            services.AddSingleton<ISchema, StarWarsSchema>();

            _provider = services.BuildServiceProvider();
            _schema = _provider.GetRequiredService<ISchema>();
            _schema.Initialize();
            _executer = new DocumentExecuter();
        }

        [Benchmark]
        public void Introspection()
        {
            var result = ExecuteQuery(_schema, Queries.Introspection);
        }

        [Benchmark]
        public void Hero()
        {
            var result = ExecuteQuery(_schema, Queries.Hero);
        }

        private ExecutionResult ExecuteQuery(ISchema schema, string query)
        {
            return _executer.ExecuteAsync(_ =>
            {
                _.Schema = schema;
                _.Query = query;
            }).GetAwaiter().GetResult();
        }

        void IBenchmark.Run() => Introspection();
    }
}
