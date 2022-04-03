using BenchmarkDotNet.Attributes;
using GraphQL.StarWars;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Benchmarks;

[MemoryDiagnoser]
//[RPlotExporter, CsvMeasurementsExporter]
public class SerializationBenchmark : IBenchmark
{
    private IServiceProvider _provider;
    private ISchema _schema;
    private DocumentExecuter _executer;

    private SystemTextJson.GraphQLSerializer _stjWriter;
    private SystemTextJson.GraphQLSerializer _stjWriterIndented;

    private NewtonsoftJson.GraphQLSerializer _nsjWriter;
    private NewtonsoftJson.GraphQLSerializer _nsjWriterIndented;

    private ExecutionResult _introspectionResult;
    private ExecutionResult _middleResult;
    private ExecutionResult _smallResult;

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
        _schema.Initialize();
        _executer = new DocumentExecuter();

        _stjWriter = new SystemTextJson.GraphQLSerializer();
        _stjWriterIndented = new SystemTextJson.GraphQLSerializer(indent: true);

        _nsjWriter = new NewtonsoftJson.GraphQLSerializer();
        _nsjWriterIndented = new NewtonsoftJson.GraphQLSerializer(indent: true);

        _introspectionResult = ExecuteQuery(_schema, Queries.Introspection);
        _smallResult = ExecuteQuery(_schema, Queries.Hero);
        _middleResult = ExecuteQuery(_schema, @"{
  hero
  {
    name
    id
    friends
    {
      id
      name
      friends
      {
        id
        name
        friends
        {
          id
          name
        }
      }
    }
  }
}");
        _stream = Stream.Null;
    }

    public IEnumerable<string> Codes => new[] { "Small", "Middle", "Introspection" };

    [ParamsSource(nameof(Codes))]
    public string Code { get; set; }

    private ExecutionResult Result => Code switch
    {
        "Small" => _smallResult,
        "Middle" => _middleResult,
        "Introspection" => _introspectionResult,
        _ => throw new NotSupportedException()
    };

    private ExecutionResult ExecuteQuery(ISchema schema, string query)
    {
        return _executer.ExecuteAsync(options =>
        {
            options.Schema = schema;
            options.Query = query;
        }).GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true)]
    public Task NewtonsoftJson() => _nsjWriter.WriteAsync(_stream, Result);

    [Benchmark]
    public Task NewtonsoftJsonIndented() => _nsjWriterIndented.WriteAsync(_stream, Result);

    [Benchmark]
    public Task SystemTextJson() => _stjWriter.WriteAsync(_stream, Result);

    [Benchmark]
    public Task SystemTextJsonIndented() => _stjWriterIndented.WriteAsync(_stream, Result);

    void IBenchmark.RunProfiler()
    {
        Code = "Introspection";
        SystemTextJson().GetAwaiter().GetResult();
    }
}
