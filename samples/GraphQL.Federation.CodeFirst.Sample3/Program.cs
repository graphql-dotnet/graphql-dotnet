using GraphQL.Federation.CodeFirst.Sample3.Schema;
using GraphQL.Transport;
using GraphQL.Types;

namespace GraphQL.Federation.CodeFirst.Sample3;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Configure services
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSingleton<Data>();
        builder.Services.AddGraphQL(b => b
            .AddSchema<Schema3>()
            .AddSystemTextJson()
            .AddGraphTypes()
            .AddFederation("2.3"));

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
