using System;
using System.IO;
using System.Threading.Tasks;
using GraphQL.Http;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace GraphQL.AspNetCore.GraphQL {
    /// <summary>
    ///     Provides middleware for hosting GraphQL.
    /// </summary>
    public sealed class GraphQLMiddleware {
        private readonly string graphqlPath;
        private readonly RequestDelegate next;
        private readonly ISchema schema;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GraphQLMiddleware" /> class.
        /// </summary>
        /// <param name="next">
        ///     The next request delegate.
        /// </param>
        /// <param name="options">
        ///     The GraphQL options.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     Throws <see cref="ArgumentNullException" /> if <paramref name="next" /> or <paramref name="options" /> is null.
        /// </exception>
        public GraphQLMiddleware(RequestDelegate next, IOptions<GraphQLOptions> options) {
            if(next == null) {
                throw new ArgumentNullException(nameof(next));
            }
            if(options == null) {
                throw new ArgumentNullException(nameof(options));
            }
            if(options.Value?.Schema == null) {
                throw new ArgumentException("Schema is null");
            }

            this.next = next;
            var optionsValue = options.Value;
            graphqlPath = string.IsNullOrEmpty(optionsValue?.GraphQLPath) ? GraphQLOptions.DefaultGraphQLPath : optionsValue.GraphQLPath;
            schema = optionsValue?.Schema;
        }

        /// <summary>
        ///     Invokes the middleware with the specified context.
        /// </summary>
        /// <param name="context">
        ///     The context.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the middleware invocation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Throws <see cref="ArgumentNullException" /> if <paramref name="context" />.
        /// </exception>
        public async Task Invoke(HttpContext context) {
            if(context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            if(ShouldRespondToRequest(context.Request)) {
                var executionResult = await ExecuteAsync(context.Request).ConfigureAwait(true);
                await WriteResponseAsync(context.Response, executionResult).ConfigureAwait(true);
            }

            await next(context).ConfigureAwait(true);
        }

        private async Task<ExecutionResult> ExecuteAsync(HttpRequest request) {
            string requestBodyText;
            using(var streamReader = new StreamReader(request.Body)) {
                requestBodyText = await streamReader.ReadToEndAsync().ConfigureAwait(true);
            }
            var graphqlRequest = JsonConvert.DeserializeObject<GraphQLRequest>(requestBodyText);
            return await new DocumentExecuter().ExecuteAsync(schema, null, graphqlRequest.Query, graphqlRequest.OperationName, graphqlRequest.Variables.ToInputs()).ConfigureAwait(true);
        }

        private bool ShouldRespondToRequest(HttpRequest request) {
            return string.Equals(request.Method, "POST", StringComparison.OrdinalIgnoreCase) && request.Path.Equals(graphqlPath);
        }

        private static Task WriteResponseAsync(HttpResponse response, ExecutionResult executionResult) {
            response.ContentType = "application/json";
            response.StatusCode = executionResult.Errors?.Count == 0 ? 200 : 400;
            var graphqlResponse = new DocumentWriter().Write(executionResult);
            return response.WriteAsync(graphqlResponse);
        }
    }
}
