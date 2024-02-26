using BenchmarkDotNet.Attributes;
using GraphQL.Caching;
using GraphQL.Execution;
using GraphQL.StarWars;
using GraphQL.Types;
using GraphQL.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Benchmarks;

[MemoryDiagnoser]
//[RPlotExporter, CsvMeasurementsExporter]
public class ExecutionBenchmark : IBenchmark
{
    private IServiceProvider _provider;
    private ISchema _schema;
    private DocumentExecuter _executer;
    private DocumentExecuter _cachedExecuter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var services = new ServiceCollection();

        services.AddGraphQL(b => b
            .AddSchema<StarWarsSchema>()
            .AddGraphTypes(typeof(StarWarsQuery).Assembly));
        services.AddSingleton<StarWarsData>();

        _provider = services.BuildServiceProvider();
        _schema = _provider.GetRequiredService<ISchema>();
        _schema.Initialize();
        _executer = new DocumentExecuter();
        _cachedExecuter = new DocumentExecuter(new GraphQLDocumentBuilder(), new DocumentValidator(), new DefaultExecutionStrategySelector(), new[] { new MemoryDocumentCache() });
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

    [Params(true, false)]
    public bool UseCaching { get; set; }

    private ExecutionResult ExecuteQuery(ISchema schema, string query)
    {
        return (UseCaching ? _cachedExecuter : _executer).ExecuteAsync(_ =>
        {
            _.Schema = schema;
            _.Query = query;
        }).GetAwaiter().GetResult();
    }

    void IBenchmark.RunProfiler() => Introspection();
}
