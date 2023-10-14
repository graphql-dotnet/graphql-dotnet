using BenchmarkDotNet.Attributes;
using GraphQL.Execution;
using GraphQL.StarWars;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Benchmarks;

[MemoryDiagnoser]
//[RPlotExporter, CsvMeasurementsExporter]
public class ValidationBenchmark : IBenchmark
{
    private IServiceProvider _provider;
    private ISchema _schema;
    private DocumentValidator _validator;

    private GraphQLDocument _introspectionDocument, _fragmentsDocument, _heroDocument;

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

        _introspectionDocument = new GraphQLDocumentBuilder().Build(Queries.Introspection);
        _fragmentsDocument = new GraphQLDocumentBuilder().Build(Queries.Fragments);
        _heroDocument = new GraphQLDocumentBuilder().Build(Queries.Hero);
    }

    [Benchmark]
    public void Introspection()
    {
        _ = Validate(_introspectionDocument);
    }

    [Benchmark]
    public void Fragments()
    {
        _ = Validate(_fragmentsDocument);
    }

    [Benchmark]
    public void Hero()
    {
        _ = Validate(_heroDocument);
    }

    private IValidationResult Validate(GraphQLDocument document) => _validator.ValidateAsync(
        new ValidationOptions
        {
            Schema = _schema,
            Document = document,
            Operation = document.Definitions.OfType<GraphQLOperationDefinition>().First()
        }).GetAwaiter().GetResult().validationResult;

    void IBenchmark.RunProfiler() => Introspection();
}
