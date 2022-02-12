using System;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Instrumentation;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Example
{
    public class GraphQLMiddleware : IMiddleware
    {
        private readonly GraphQLSettings _settings;
        private readonly IDocumentExecuter _executer;
        private readonly IDocumentWriter _writer;
        private readonly ISchema _schema;

        public GraphQLMiddleware(
            IOptions<GraphQLSettings> options,
            IDocumentExecuter executer,
            IDocumentWriter writer,
            ISchema schema)
        {
            _settings = options.Value;
            _executer = executer;
            _writer = writer;
            _schema = schema;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (!IsGraphQLRequest(context))
            {
                await next(context);
                return;
            }

            await ExecuteAsync(context);
        }

        private bool IsGraphQLRequest(HttpContext context)
        {
            return context.Request.Path.StartsWithSegments(_settings.GraphQLPath)
                && string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase);
        }

        private async Task ExecuteAsync(HttpContext context)
        {
            var start = DateTime.UtcNow;

            var request = await context.Request.Body.FromJsonAsync<GraphQLRequest>(context.RequestAborted);

            var result = await _executer.ExecuteAsync(options =>
            {
                options.Schema = _schema;
                options.Query = request.Query;
                options.OperationName = request.OperationName;
                options.Inputs = request.Variables;
                options.UserContext = _settings.BuildUserContext?.Invoke(context);
                options.EnableMetrics = _settings.EnableMetrics;
                options.RequestServices = context.RequestServices;
                options.CancellationToken = context.RequestAborted;
            });

            if (_settings.EnableMetrics)
            {
                result.EnrichWithApolloTracing(start);
            }

            await WriteResponseAsync(context, result, context.RequestAborted);
        }

        private async Task WriteResponseAsync(HttpContext context, ExecutionResult result, CancellationToken cancellationToken)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 200; // OK

            await _writer.WriteAsync(context.Response.Body, result, cancellationToken);
        }
    }
}
