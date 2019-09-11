using BenchmarkDotNet.Attributes;
using GraphQL.Introspection;
using GraphQL.StarWars;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace GraphQL.Benchmarks
{
    [MemoryDiagnoser]
    public class ExecutionBenchmark
    {
        private readonly IServiceProvider _provider;
        private readonly ISchema _schema;
        private readonly DocumentExecuter _executer = new DocumentExecuter();

        public ExecutionBenchmark()
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
        }

        [Benchmark(Description = "Introspection Query")]
        public void Introspection()
        {
            var result = ExecuteQuery(_schema, SchemaIntrospection.IntrospectionQuery);
        }

        [Benchmark(Description = "Small Query")]
        public void Hero()
        {
            var result = ExecuteQuery(_schema, "hero { id name }");
        }

        private ExecutionResult ExecuteQuery(ISchema schema, string query)
        {
            return _executer.ExecuteAsync(_ =>
            {
                _.Schema = schema;
                _.Query = query;
            }).GetAwaiter().GetResult();
        }
    }
}
