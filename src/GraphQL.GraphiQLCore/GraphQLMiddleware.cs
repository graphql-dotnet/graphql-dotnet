using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GraphQL.Http;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace GraphQL.GraphiQLCore
{
    public class GraphQLMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly GraphQLSettings _settings;
        private readonly IDocumentExecuter _executer;
        private readonly IDocumentWriter _writer;

        public GraphQLMiddleware(
            RequestDelegate next,
            GraphQLSettings settings,
            IDocumentExecuter executer,
            IDocumentWriter writer)
        {
            _next = next;
            _settings = settings;
            _executer = executer;
            _writer = writer;
        }

        public async Task Invoke(HttpContext context, ISchema schema)
        {
            if (!IsGraphQLRequest(context))
            {
                if (IsOptionsRequest(context))
                {
                    SendOptionsRequest(context);
                }
                else
                {
                    await _next(context);
                }
                return;
            };

            await ExecuteAsync(context, schema);
        }

        private bool IsGraphQLRequest(HttpContext context)
        {
            return context.Request.Path.StartsWithSegments(_settings.Path)
                && string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsOptionsRequest(HttpContext context)
        {
            return (string.Equals(context.Request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(context.Request.Headers.FirstOrDefault(s => s.Key == "Access-Control-Request-Method").Value,
                        "POST", StringComparison.OrdinalIgnoreCase));
        }

        private void SendOptionsRequest(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.Headers.Add("Cache-Control", "no-cache");
            context.Response.Headers.Add("Content-Length", "0");
            context.Response.Headers.Add("Expires", "-1");
            context.Response.Headers.Add("Pragma", "no-cache");
            context.Response.Headers.Add("Access-Control-Allow-Headers", "access-control-allow-origin,content-type");
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        }

        private async Task ExecuteAsync(HttpContext context, ISchema schema)
        {
            string body;
            using (var streamReader = new StreamReader(context.Request.Body))
            {
                body = await streamReader.ReadToEndAsync().ConfigureAwait(true);
            }

            var request = JsonConvert.DeserializeObject<GraphQLRequest>(body);

            var result = await _executer.ExecuteAsync(_ =>
            {
                _.Schema = schema;
                _.Query = request.Query;
                _.OperationName = request.OperationName;
                _.Inputs = request.Variables.ToInputs();
                _.UserContext = _settings.BuildUserContext?.Invoke(context);
            });

            await WriteResponseAsync(context, result);
        }

        private async Task WriteResponseAsync(HttpContext context, ExecutionResult result)
        {
            var json = _writer.Write(result);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = result.Errors?.Any() == true ? (int)HttpStatusCode.BadRequest : (int)HttpStatusCode.OK;
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            await context.Response.WriteAsync(json);
        }
    }
}
