using BenchmarkDotNet.Attributes;
using GraphQL.Execution;
using GraphQL.StarWars;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using GraphQL.Types.Relay;
using GraphQL.Validation;
using GraphQL.Validation.Rules;
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
    private ISchema _overlappingFieldsSchema;
    private GraphQLDocument _overlappingFieldsDocument;

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
        services.AddSingleton<ConnectionType<CharacterInterface, EdgeType<CharacterInterface>>>();
        services.AddSingleton<EdgeType<CharacterInterface>>();
        services.AddSingleton<PageInfoType>();

        _provider = services.BuildServiceProvider();
        _schema = _provider.GetRequiredService<ISchema>();
        _schema.Initialize();
        _validator = new DocumentValidator();

        _introspectionDocument = new GraphQLDocumentBuilder().Build(Queries.Introspection);
        _fragmentsDocument = new GraphQLDocumentBuilder().Build(Queries.Fragments);
        _heroDocument = new GraphQLDocumentBuilder().Build(Queries.Hero);

        _overlappingFieldsSchema = Schema.For("""
            type Query { field: Node }
            type Node { f: Node, g: Node, x: String }
            """);
        const int n = 100;
        const int m = 50;
        var inner = string.Join(" ", Enumerable.Repeat("... on Node { x }", m));
        var outer = string.Join(" ", Enumerable.Repeat($"... on Node {{ f {{ {inner} }} }}", n));
        var query = $"{{ field {{ {outer} }} }}";
        _overlappingFieldsDocument = new GraphQLDocumentBuilder().Build(query);
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

    [Benchmark]
    public void ManyInlineFragments()
    {
        _ = _validator.ValidateAsync(new ValidationOptions
        {
            Schema = _overlappingFieldsSchema,
            Document = _overlappingFieldsDocument,
            Rules = [OverlappingFieldsCanBeMerged.Instance],
            Operation = _overlappingFieldsDocument.Definitions.OfType<GraphQLOperationDefinition>().First(),
        }).GetAwaiter().GetResult();
    }

    private IValidationResult Validate(GraphQLDocument document) => _validator.ValidateAsync(
        new ValidationOptions
        {
            Schema = _schema,
            Document = document,
            Operation = document.Definitions.OfType<GraphQLOperationDefinition>().First()
        }).GetAwaiter().GetResult();

    void IBenchmark.RunProfiler() => ManyInlineFragments();
}
