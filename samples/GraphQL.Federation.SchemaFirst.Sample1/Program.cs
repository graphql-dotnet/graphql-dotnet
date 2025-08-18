using System.Reflection;
using GraphQL.Federation.SchemaFirst.Sample1.Schema;
using GraphQL.Transport;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Federation.SchemaFirst.Sample1;

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
            .AddSystemTextJson()
            .AddFederation("1.0")); // specify Federation v1 compatibility

        // Build the web application
        var app = builder.Build();

        // Some simple GraphQL middleware (NOTE: it is suggested to use the GraphQL.Server.Transports.AspNetCore package instead)
        app.MapPost("/graphql", GraphQLHttpMiddlewareAsync);

        // Add a UI package for testing
        app.UseGraphQLGraphiQL("/");

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
        // load the schema-first SDL from an embedded resource
        var filename = "GraphQL.Federation.SchemaFirst.Sample1.Schema.gql";
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(filename)
            ?? throw new InvalidOperationException("Could not read schema definitions from embedded resource.");
        var reader = new StreamReader(stream);
        var schemaString = reader.ReadToEnd();

        // note: this demonstrates GraphQL.NET v8 and newer configuration methods
        // define the known types and their resolvers
        var schemaBuilder = new SchemaBuilder
        {
            ServiceProvider = serviceProvider,
        };
        schemaBuilder.Types.Include<Query>();
        schemaBuilder.Types.Include<Category>();
        schemaBuilder.Types.For(nameof(Category)).ResolveReference(
            new MyFederatedResolver<Category>((data, id) => data.GetCategoryById(id)));

        // build the schema
        return schemaBuilder.Build(schemaString);
    }

    private static async Task GraphQLHttpMiddlewareAsync(HttpContext context)
    {
        // NOTE: it is suggested to use the GraphQL.Server.Transports.AspNetCore package instead

        // pull the serializer and executer from the DI container
        var serializer = context.RequestServices.GetRequiredService<IGraphQLSerializer>();
        var executer = context.RequestServices.GetRequiredService<IDocumentExecuter>();

        // read the request (ignores the content-type header)
        var request = await serializer.ReadAsync<GraphQLRequest>(context.Request.Body, context.RequestAborted).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Could not read request");

        // execute the request
        var result = await executer.ExecuteAsync(options =>
        {
            options.Query = request.Query;
            options.OperationName = request.OperationName;
            options.Variables = request.Variables;
            options.RequestServices = context.RequestServices;
            options.CancellationToken = context.RequestAborted;
        }).ConfigureAwait(false);

        // write the response
        context.Response.StatusCode = 200; // always OK even for errors
        context.Response.ContentType = "application/json";
        await serializer.WriteAsync(context.Response.Body, result, context.RequestAborted).ConfigureAwait(false);
    }
}
