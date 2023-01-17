// See https://aka.ms/new-console-template for more information
using GraphQL;
using GraphQL.StarWars.TypeFirst;
using GraphQL.StarWars.TypeFirst.Types;
using GraphQL.Types;
using GraphQL.Types.Relay.DataObjects;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Sample of AOT compilation of a GraphQL query on a type-first schema");
Console.WriteLine();

IServiceCollection serviceCollection = new ServiceCollection();
// build services as usual; most methods are fully supported except:
//   - AddClrTypeMappings
//   - AddAutoClrMappings
//   - AddAutoSchema
serviceCollection.AddGraphQL(b => b
    .AddSystemTextJson()
    .AddAutoSchema<StarWarsQuery>(c => c.WithMutation<StarWarsMutation>())
);

#pragma warning disable IL2111 // Method with parameters or return value with `DynamicallyAccessedMembersAttribute` is accessed via reflection. Trimmer can't guarantee availability of the requirements of the method.
// all types within GraphQL.StarWars.TypeFirst are rooted via TrimmerRootAssembly in the csproj so they will not be trimmed
// for enumeration types, must also root these two types (for each enum in the schema)
Preserve<GraphQLClrOutputTypeReference<Episodes>>();
Preserve<EnumerationGraphType<Episodes>>();
// for connection types, must also root the connection types used in the schema
Preserve<Connection<IStarWarsCharacter>>();
Preserve<Edge<IStarWarsCharacter>>();
Preserve<PageInfo>();
#pragma warning restore IL2111 // Method with parameters or return value with `DynamicallyAccessedMembersAttribute` is accessed via reflection. Trimmer can't guarantee availability of the requirements of the method.

serviceCollection.AddSingleton<StarWarsData>();

// must manually register the query and mutation types or AOT will trim their constructors
// all other graph types' constructors are preserved via calls to Field<T>
//serviceCollection.AddTransient<StarWarsQuery>();
//serviceCollection.AddTransient<StarWarsMutation>();

// other notes:
// - auto clr type mappings are generally not supported
// - auto registering graph types are generally not supported
// - field builders that use Expressions as resolvers such as Field(x => x.Name) are discouraged
// - field builders that do not include a resolver such as Field<StringGraphType>("Name") are not supported
// - strongly recommend each field has the explicit graph type specified and a resolver specified

#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
var services = serviceCollection.BuildServiceProvider();
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.

var executer = services.GetRequiredService<IDocumentExecuter>();

var ret = await executer.ExecuteAsync(new ExecutionOptions
{
    Schema = services.GetRequiredService<ISchema>(),
    Query = "{ hero { id name } }",
    RequestServices = services,
    ThrowOnUnhandledException = true,
}).ConfigureAwait(false);

var serializer = services.GetRequiredService<IGraphQLTextSerializer>();
var response = serializer.Serialize(ret);

Console.WriteLine("Executing request: { hero { id name } }");
Console.WriteLine();
Console.WriteLine(response);
Console.WriteLine();

if (response != """{"data":{"hero":{"id":"3","name":"R2-D2"}}}""")
{
    Console.WriteLine("Unexpected response; exiting.");
    return 1; // return application exit code of 1 indicating failure
}

var introspectionQuery = LoadResource("IntrospectionQuery.graphql");


ret = await executer.ExecuteAsync(new ExecutionOptions
{
    Schema = services.GetRequiredService<ISchema>(),
    Query = introspectionQuery,
    RequestServices = services,
    ThrowOnUnhandledException = true,
}).ConfigureAwait(false);

response = serializer.Serialize(ret);

Console.WriteLine("Executing introspection query:");
Console.WriteLine();
Console.WriteLine(response);

// return an application exit code if there were any errors
return ret.Errors?.Count ?? 0;

static string LoadResource(string resourceName)
{
    using var stream = typeof(Program).Assembly.GetManifestResourceStream("GraphQL.AotCompilationSample.TypeFirst." + resourceName)!;
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
}

void Preserve<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() => Preserve2(typeof(T));
void Preserve2([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type t) => GC.KeepAlive(t);
