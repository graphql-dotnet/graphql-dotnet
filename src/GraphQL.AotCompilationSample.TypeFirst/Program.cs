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
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
serviceCollection.AddGraphQL(b => b
    .AddSystemTextJson()
    .AddAutoSchema<StarWarsQuery>(c => c.WithMutation<StarWarsMutation>())
);
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

#pragma warning disable IL2111 // Method with parameters or return value with `DynamicallyAccessedMembersAttribute` is accessed via reflection. Trimmer can't guarantee availability of the requirements of the method.
// All CLR types for the schema (within GraphQL.StarWars.TypeFirst) must be rooted in the csproj
// file via TrimmerRootAssembly or else they will be trimmed by the linker, and the auto-registering
// graph types will not find any properties/methods to register. However, any services such as the
// StarWarsData service do not need to be rooted in the csproj file, as the linker will intelligently
// preserve the service's constructor (due to AddSingleton) and any methods that are called on it.
// For enumeration types, must also root these two types (for each enum in the schema)
Preserve<GraphQLClrOutputTypeReference<Episodes>>();
Preserve<EnumerationGraphType<Episodes>>();
// for connection types, must also root the connection types used in the schema, as they do
// not exist within the GraphQL.StarWars.TypeFirst assembly
Preserve<Connection<IStarWarsCharacter>>();
Preserve<Edge<IStarWarsCharacter>>();
Preserve<PageInfo>();
#pragma warning restore IL2111 // Method with parameters or return value with `DynamicallyAccessedMembersAttribute` is accessed via reflection. Trimmer can't guarantee availability of the requirements of the method.

serviceCollection.AddSingleton<StarWarsData>();

// other notes:
// - auto clr type mappings are generally not supported
// - auto registering graph types are generally not supported
// - field builders that use Expressions as resolvers such as Field(x => x.Name) are discouraged
// - field builders that do not include a resolver such as Field<StringGraphType>("Name") are not supported
// - strongly recommend each field has the explicit graph type specified and a resolver specified

#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
using var services = serviceCollection.BuildServiceProvider();
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.

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
    using var stream = typeof(Program).Assembly.GetManifestResourceStream("GraphQL.AotCompilationSample.TypeFirst." + resourceName)!;
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
}

// This 'roots' the specified type, forcing the trimmer to retain the specified type and all its members.
// 'Rooting' the type also means that all of its statically understood dependencies will be retained as well. 
void Preserve<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() => GC.KeepAlive(typeof(T));
