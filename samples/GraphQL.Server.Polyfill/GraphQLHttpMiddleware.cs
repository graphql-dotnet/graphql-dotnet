using GraphQL.Transport;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Polyfill;

// This is just a simple example of ASP.NET Core middleware to setup GraphQL.NET execution engine.
// It is not intended to be used in production. We recommend to use middleware from server project.
// See https://github.com/graphql-dotnet/server.
public class GraphQLHttpMiddleware<TSchema>
    where TSchema : ISchema
{
    private readonly IDocumentExecuter<TSchema> _executer;
    private readonly IGraphQLSerializer _serializer;
    private readonly RequestDelegate _next;

    public GraphQLHttpMiddleware(
        IDocumentExecuter<TSchema> executer,
        IGraphQLSerializer serializer,
        RequestDelegate next)
    {
        _executer = executer;
        _serializer = serializer;
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsGraphQLRequest(context))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        await ExecuteAsync(context).ConfigureAwait(false);
    }

    private bool IsGraphQLRequest(HttpContext context)
    {
        return string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase);
    }

    private async Task ExecuteAsync(HttpContext context)
    {
        var start = DateTime.UtcNow;

        var request = await _serializer.ReadAsync<GraphQLRequest>(context.Request.Body, context.RequestAborted).ConfigureAwait(false);

        var result = await _executer.ExecuteAsync(options =>
        {
            options.Query = request!.Query;
            options.OperationName = request.OperationName;
            options.Variables = request.Variables;
            options.RequestServices = context.RequestServices;
            options.CancellationToken = context.RequestAborted;
            options.User = context.User;
        }).ConfigureAwait(false);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 200; // OK

        await _serializer.WriteAsync(context.Response.Body, result, context.RequestAborted).ConfigureAwait(false);
    }
}
