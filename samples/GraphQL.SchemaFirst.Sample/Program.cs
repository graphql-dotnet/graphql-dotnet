using System.Reflection;
using GraphQL;
using GraphQL.Types;
using GraphQL.Utilities;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Schema-first sample");
Console.WriteLine();

var services = new ServiceCollection();
services.AddSingleton<DroidRepository>();
services.AddSingleton<Query>();
services.AddGraphQL(builder => builder
    .AddSchema(BuildSchema)
    .AddSystemTextJson());

using var provider = services.BuildServiceProvider();

var executer = provider.GetRequiredService<IDocumentExecuter>();
var serializer = provider.GetRequiredService<IGraphQLTextSerializer>();
var schema = provider.GetRequiredService<ISchema>();

const string query = """
{
  hero {
    id
    name
    primaryFunction
    appearsIn
  }
}
""";

Console.WriteLine("Executing request:");
Console.WriteLine(query);
Console.WriteLine();

var result = await executer.ExecuteAsync(new ExecutionOptions
{
    Schema = schema,
    Query = query,
    RequestServices = provider,
    ThrowOnUnhandledException = true,
}).ConfigureAwait(false);

var response = serializer.Serialize(result);
Console.WriteLine(response);

const string expected = """{"data":{"hero":{"id":"3","name":"R2-D2","primaryFunction":"Astromech","appearsIn":["NEWHOPE","EMPIRE","JEDI"]}}}""";
if (response != expected)
{
    Console.WriteLine("Unexpected response; exiting.");
    return 1;
}

return result.Errors?.Count ?? 0;

static ISchema BuildSchema(IServiceProvider serviceProvider)
{
    var schemaBuilder = new SchemaBuilder
    {
        ServiceProvider = serviceProvider,
    };
    schemaBuilder.Types.Include<Query>();

    return schemaBuilder.Build(LoadResource("Schema.gql"));
}

static string LoadResource(string resourceName)
{
    using var stream = Assembly.GetExecutingAssembly()
        .GetManifestResourceStream("GraphQL.SchemaFirst.Sample." + resourceName)
        ?? throw new InvalidOperationException("Could not read schema definitions from embedded resource.");
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
}

public class Query
{
    public Droid Hero([FromServices] DroidRepository repository)
        => repository.GetHero();

    public Droid? Droid(string id, [FromServices] DroidRepository repository)
        => repository.GetDroid(id);
}

public class DroidRepository
{
    private readonly IReadOnlyList<Droid> _droids =
    [
        new("3", "R2-D2", "Astromech", ["NEWHOPE", "EMPIRE", "JEDI"]),
        new("4", "C-3PO", "Protocol", ["NEWHOPE", "EMPIRE", "JEDI"]),
    ];

    public Droid GetHero()
        => _droids[0];

    public Droid? GetDroid(string id)
        => _droids.FirstOrDefault(droid => droid.Id == id);
}

public record Droid(string Id, string Name, string PrimaryFunction, IReadOnlyList<string> AppearsIn);
