using GraphQL;
using GraphQL.Instrumentation;
using GraphQL.Transport;
using GraphQL.Types;
using Microsoft.Extensions.Options;

namespace Example;

// This is just a simple example of ASP.NET Core middleware to setup GraphQL.NET execution engine.
// It is not intended to be used in production. We recommend to use middleware from server project.
// See https://github.com/graphql-dotnet/server.
public class GraphQlMiddleware(
    IOptions<GraphQlSettings> options,
    IDocumentExecuter executer,
    IGraphQLSerializer serializer,
    ISchema schema)
    : IMiddleware
{
    private readonly GraphQlSettings _settings = options.Value;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!IsGraphQlRequest(context))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        await ExecuteAsync(context).ConfigureAwait(false);
    }

    private bool IsGraphQlRequest(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments(_settings.GraphQlPath)
            && string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase);
    }

    private async Task ExecuteAsync(HttpContext context)
    {
        var start = DateTime.UtcNow;

        var request = await serializer.ReadAsync<GraphQLRequest>(context.Request.Body, context.RequestAborted).ConfigureAwait(false);

        var result = await executer.ExecuteAsync(options =>
        {
            options.Schema = schema;
            options.Query = request!.Query;
            options.OperationName = request.OperationName;
            options.Variables = request.Variables;
            options.UserContext = _settings.BuildUserContext!.Invoke(context)!;
            options.EnableMetrics = _settings.EnableMetrics;
            options.RequestServices = context.RequestServices;
            options.CancellationToken = context.RequestAborted;
        }).ConfigureAwait(false);

        if (_settings.EnableMetrics)
        {
            result.EnrichWithApolloTracing(start);
        }

        await WriteResponseAsync(context, result, context.RequestAborted).ConfigureAwait(false);
    }

    private async Task WriteResponseAsync(HttpContext context, ExecutionResult result, CancellationToken cancellationToken)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 200; // OK

        await serializer.WriteAsync(context.Response.Body, result, cancellationToken).ConfigureAwait(false);
    }
}
