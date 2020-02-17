using System;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using GraphQL.Introspection;
using GraphQL.StarWars;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Benchmarks
{
    [MemoryDiagnoser]
    [RPlotExporter, CsvMeasurementsExporter]
    public class SerializationBenchmark
    {
        private IServiceProvider _provider;
        private ISchema _schema;
        private DocumentExecuter _executer;

        private SystemTextJson.DocumentWriter _stjWriter;
        private SystemTextJson.DocumentWriter _stjWriterIndented;

        private NewtonsoftJson.DocumentWriter _nsjWriter;
        private NewtonsoftJson.DocumentWriter _nsjWriterIndented;

        private ExecutionResult _result;
        private Stream _stream;

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
            _executer = new DocumentExecuter();

            _stjWriter = new SystemTextJson.DocumentWriter();
            _stjWriterIndented = new SystemTextJson.DocumentWriter(indent: true);

            _nsjWriter = new NewtonsoftJson.DocumentWriter();
            _nsjWriterIndented = new NewtonsoftJson.DocumentWriter(indent: true);

            _result = ExecuteQuery(_schema, SchemaIntrospection.IntrospectionQuery);
            _stream = Stream.Null;
        }

        private ExecutionResult ExecuteQuery(ISchema schema, string query)
        {
            return _executer.ExecuteAsync(options =>
            {
                options.Schema = schema;
                options.Query = query;
            }).GetAwaiter().GetResult();
        }

        [Benchmark(Baseline = true)]
        public Task NewtonsoftJson() => _nsjWriter.WriteAsync(_stream, _result);

        [Benchmark]
        public Task NewtonsoftJsonIndented() => _nsjWriterIndented.WriteAsync(_stream, _result);

        [Benchmark]
        public Task SystemTextJson() => _stjWriter.WriteAsync(_stream, _result);

        [Benchmark]
        public Task SystemTextJsonIndented() => _stjWriterIndented.WriteAsync(_stream, _result);
    }
}
