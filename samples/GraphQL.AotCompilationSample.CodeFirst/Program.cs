// See https://aka.ms/new-console-template for more information
using GraphQL;
using GraphQL.StarWars;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Sample of AOT compilation of a GraphQL query");
Console.WriteLine();

IServiceCollection serviceCollection = new ServiceCollection();
// build services as usual; most methods are fully supported except:
//   - AddClrTypeMappings
//   - AddAutoClrMappings
//   - AddAutoSchema
serviceCollection.AddGraphQL(b => b
    .AddSystemTextJsonAot()
    //.AddSchema<StarWarsSchema>()
    //.AddGraphTypes(typeof(StarWarsSchema).Assembly)
    .AddSelfActivatingSchema<StarWarsSchema>()
);

serviceCollection.AddSingleton<StarWarsData>();

// must manually register the query and mutation types or AOT will trim their constructors
// all other graph types' constructors are preserved via calls to Field<T>
serviceCollection.AddTransient<StarWarsQuery>();
serviceCollection.AddTransient<StarWarsMutation>();

// other notes:
// - auto clr type mappings are generally not supported
// - auto registering graph types are generally not supported
// - field builders that use Expressions as resolvers such as Field(x => x.Name) are discouraged
// - field builders that do not include a resolver such as Field<StringGraphType>("Name") are not supported
// - strongly recommend each field has the explicit graph type specified and a resolver specified

using var services = serviceCollection.BuildServiceProvider();

var executer = services.GetRequiredService<IDocumentExecuter>();

Console.WriteLine("Executing request: { hero { id name } }");
Console.WriteLine();

var ret = await executer.ExecuteAsync(new ExecutionOptions
{
    Schema = services.GetRequiredService<ISchema>(),
    Query = "{ hero { id name } }",
    RequestServices = services,
    ThrowOnUnhandledException = true,
}).ConfigureAwait(false);

var serializer = services.GetRequiredService<IGraphQLTextSerializer>();
var response = serializer.Serialize(ret);

Console.WriteLine(response);
Console.WriteLine();

if (response != """{"data":{"hero":{"id":"3","name":"R2-D2"}}}""")
{
    Console.WriteLine("Unexpected response; exiting.");
    return 1; // return application exit code of 1 indicating failure
}

var introspectionQuery = LoadResource("IntrospectionQuery.graphql");

Console.WriteLine("Executing introspection query:");
Console.WriteLine();

ret = await executer.ExecuteAsync(new ExecutionOptions
{
    Schema = services.GetRequiredService<ISchema>(),
    Query = introspectionQuery,
    RequestServices = services,
    ThrowOnUnhandledException = true,
}).ConfigureAwait(false);

response = serializer.Serialize(ret);

Console.WriteLine(response);

// return an application exit code if there were any errors
return ret.Errors?.Count ?? 0;

static string LoadResource(string resourceName)
{
    using var stream = typeof(Program).Assembly.GetManifestResourceStream("GraphQL.AotCompilationSample.CodeFirst." + resourceName)!;
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
}
