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

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
serviceCollection.AddGraphQL(b => b
    .AddSystemTextJson()
    .AddAutoSchema<StarWarsQuery>(c => c.WithMutation<StarWarsMutation>())
    .ConfigureSchema(s =>
    {
        // All CLR types for the schema (within GraphQL.StarWars.TypeFirst) must be rooted in the csproj
        // file via TrimmerRootAssembly or else they will be trimmed by the linker, and the auto-registering
        // graph types will not find any properties/methods to register. However, any services such as the
        // StarWarsData service do not need to be rooted in the csproj file, as the linker will intelligently
        // preserve the service's constructor (due to AddSingleton) and any methods that are called on it.

        // For enumeration types, and types not in the GraphQL.StarWars.TypeFirst assembly, manually calling
        // RegisterTypeMapping or AutoRegister will root the proper classes necessary for the schema to work.
        s.RegisterTypeMapping<Episodes, EnumerationGraphType<Episodes>>();
        s.AutoRegister<Connection<IStarWarsCharacter>>();
        s.AutoRegister<Edge<IStarWarsCharacter>>();
        s.AutoRegister<PageInfo>();
    })
);
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

serviceCollection.AddSingleton<StarWarsData>();

// for enumeration types, although the EnumerationGraphType<Episodes> type has been properly rooted, the
// .NET 8 DI provider will refuse to create open generic types of value types, so they must be registered manually
serviceCollection.AddTransient<EnumerationGraphType<Episodes>>();

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
    using var stream = typeof(Program).Assembly.GetManifestResourceStream("GraphQL.AotCompilationSample.TypeFirst." + resourceName)!;
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
}
