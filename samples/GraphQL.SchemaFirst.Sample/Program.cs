using System.Reflection;
using GraphQL.Transport;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.SchemaFirst.Sample;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSingleton<StarWarsData>();
        builder.Services.AddSingleton<Query>();
        builder.Services.AddSingleton<Mutation>();
        builder.Services.AddGraphQL(b => b
            .AddSchema(BuildSchema)
            .AddSystemTextJson());

        var app = builder.Build();

        app.MapPost("/graphql", GraphQLHttpMiddlewareAsync);

        app.UseGraphQLGraphiQL("/");

        {
            var schema = app.Services.GetRequiredService<ISchema>();
            schema.Initialize();
        }

        await app.RunAsync().ConfigureAwait(false);
    }

    private static ISchema BuildSchema(IServiceProvider serviceProvider)
    {
        var filename = "GraphQL.SchemaFirst.Sample.StarWarsSchema.gql";
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(filename)
            ?? throw new InvalidOperationException("Could not read schema definitions from embedded resource.");
        using var reader = new StreamReader(stream);
        var schemaString = reader.ReadToEnd();

        var schemaBuilder = new SchemaBuilder
        {
            ServiceProvider = serviceProvider,
        };
        schemaBuilder.Types.Include<Query>();
        schemaBuilder.Types.Include<Mutation>();

        var schema = schemaBuilder.Build(schemaString);
        return schema;
    }

    private static async Task GraphQLHttpMiddlewareAsync(HttpContext context)
    {
        var serializer = context.RequestServices.GetRequiredService<IGraphQLSerializer>();
        var executer = context.RequestServices.GetRequiredService<IDocumentExecuter>();

        var request = await serializer.ReadAsync<GraphQLRequest>(context.Request.Body, context.RequestAborted).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Could not read request");

        var result = await executer.ExecuteAsync(options =>
        {
            options.Query = request.Query;
            options.OperationName = request.OperationName;
            options.Variables = request.Variables;
            options.RequestServices = context.RequestServices;
            options.CancellationToken = context.RequestAborted;
        }).ConfigureAwait(false);

        context.Response.StatusCode = 200;
        context.Response.ContentType = "application/json";
        await serializer.WriteAsync(context.Response.Body, result, context.RequestAborted).ConfigureAwait(false);
    }
}
