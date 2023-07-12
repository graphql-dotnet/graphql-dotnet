using System.Reflection;
using GraphQL.Federation.SchemaFirst.Sample2.Schema;
using GraphQL.Transport;
using GraphQL.Types;
using GraphQL.Utilities.Federation;

namespace GraphQL.Federation.SchemaFirst.Sample2;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Configure services
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSingleton<Data>();
        builder.Services.AddSingleton<Query>();
        builder.Services.AddGraphQL(b => b
            .AddSchema(BuildSchema)
            .AddSystemTextJson());

        // Build the web application
        var app = builder.Build();

        // Some simple GraphQL middleware
        app.MapPost("/graphql", GraphQLHttpMiddlewareAsync);

        // Add a UI package for testing
        app.UseGraphQLPlayground("/");

        // Optional: ensure that the schema builds and initializes
        {
            var schema = app.Services.GetRequiredService<ISchema>();
            schema.Initialize();
        }

        // Start the application
        await app.RunAsync().ConfigureAwait(false);
    }

    private static ISchema BuildSchema(IServiceProvider serviceProvider)
    {
        var data = serviceProvider.GetRequiredService<Data>();
        var filename = "GraphQL.Federation.SchemaFirst.Sample2.Schema.gql";
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(filename)
            ?? throw new InvalidOperationException("Could not read schema definitions from embedded resource.");
        var reader = new StreamReader(stream);
        var schemaString = reader.ReadToEnd();
        var schemaBuilder = new FederatedSchemaBuilder();
        schemaBuilder.Types.Include<Query>();
        schemaBuilder.Types.Include<Category>();
        schemaBuilder.Types.For(nameof(Category)).ResolveReferenceAsync(data.GetResolver<Category>());
        schemaBuilder.Types.Include<Product>();
        schemaBuilder.Types.For(nameof(Product)).ResolveReferenceAsync(data.GetResolver<Product>());
        return schemaBuilder.Build(schemaString);
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
