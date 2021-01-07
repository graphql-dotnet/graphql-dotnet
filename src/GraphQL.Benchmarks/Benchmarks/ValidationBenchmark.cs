using System;
using BenchmarkDotNet.Attributes;
using GraphQL.Execution;
using GraphQL.Language.AST;
using GraphQL.StarWars;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using GraphQL.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Benchmarks
{
    [MemoryDiagnoser]
    //[RPlotExporter, CsvMeasurementsExporter]
    public class ValidationBenchmark : IBenchmark
    {
        private IServiceProvider _provider;
        private ISchema _schema;
        private DocumentValidator _validator;

        private Document _introspectionDocument, _heroDocument;

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
            _validator = new DocumentValidator();

            _introspectionDocument = new GraphQLDocumentBuilder().Build(SchemaIntrospection.IntrospectionQuery);
            _heroDocument = new GraphQLDocumentBuilder().Build("{ hero { id name } }");
        }

        [Benchmark]
        public void Introspection()
        {
            _ = Validate(_introspectionDocument);
        }

        [Benchmark]
        public void Hero()
        {
            _ = Validate(_heroDocument);
        }

        private IValidationResult Validate(Document document) => _validator.ValidateAsync(null, _schema, document).GetAwaiter().GetResult();

        void IBenchmark.Run() => Introspection();
    }
}
