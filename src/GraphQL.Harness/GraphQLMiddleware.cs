using System;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Instrumentation;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Example
{
    public class GraphQLMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly GraphQLSettings _settings;
        private readonly IDocumentExecuter _executer;
        private readonly IDocumentWriter _writer;
        private readonly IGraphQLRequestReader _reader;

        public GraphQLMiddleware(
            RequestDelegate next,
            IOptions<GraphQLSettings> options,
            IDocumentExecuter executer,
            IDocumentWriter writer,
            IGraphQLRequestReader reader)
        {
            _next = next;
            _settings = options.Value;
            _executer = executer;
            _writer = writer;
            _reader = reader;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ASP.NET Core convention")]
        public async Task Invoke(HttpContext context, ISchema schema)
        {
            if (!IsGraphQLRequest(context))
            {
                await _next(context);
                return;
            }

            await ExecuteAsync(context, schema);
        }

        private bool IsGraphQLRequest(HttpContext context)
        {
            return context.Request.Path.StartsWithSegments(_settings.GraphQLPath)
                && string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase);
        }

        private async Task ExecuteAsync(HttpContext context, ISchema schema)
        {
            var start = DateTime.UtcNow;

            var request = await _reader.ReadAsync<GraphQLRequest>(context.Request.Body, context.RequestAborted);

            var result = await _executer.ExecuteAsync(options =>
            {
                options.Schema = schema;
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
