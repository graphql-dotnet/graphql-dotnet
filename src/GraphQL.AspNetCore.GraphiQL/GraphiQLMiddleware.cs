using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GraphQL.AspNetCore.GraphiQL {
    /// <summary>
    ///     Provides middleware to display GraphiQL.
    /// </summary>
    public sealed class GraphiQLMiddleware {
        private static readonly string Template = ReadTemplate();
        private readonly string graphiqlPath;
        private readonly string graphqlPath;
        private readonly RequestDelegate next;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GraphiQLMiddleware" /> class.
        /// </summary>
        /// <param name="next">
        ///     The next request delegate.
        /// </param>
        /// <param name="options">
        ///     The GraphiQL options.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     Throws <see cref="ArgumentNullException" /> if <paramref name="next" /> or <paramref name="options" /> is null.
        /// </exception>
        public GraphiQLMiddleware(RequestDelegate next, IOptions<GraphiQLOptions> options) {
            if(next == null) {
                throw new ArgumentNullException(nameof(next));
            }
            if(options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            this.next = next;
            var optionsValue = options.Value;
            graphiqlPath = string.IsNullOrEmpty(optionsValue?.GraphiQLPath) ? GraphiQLOptions.DefaultGraphiQLPath : optionsValue.GraphiQLPath;
            graphqlPath = string.IsNullOrEmpty(optionsValue?.GraphQLPath) ? GraphiQLOptions.DefaultGraphQLPath : optionsValue.GraphQLPath;
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
                await WriteResponseAsync(context.Response).ConfigureAwait(true);
            }

            await next(context).ConfigureAwait(true);
        }

        private static string ReadTemplate() {
            var assembly = typeof(GraphiQLMiddleware).GetTypeInfo().Assembly;
            using(var stream = assembly.GetManifestResourceStream("GraphQL.AspNetCore.GraphiQL.index.html"))
            using(var streamReader = new StreamReader(stream, Encoding.UTF8)) {
                return streamReader.ReadToEnd();
            }
        }

        private bool ShouldRespondToRequest(HttpRequest request) {
            return string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase) && request.Path.Equals(graphiqlPath);
        }

        private Task WriteResponseAsync(HttpResponse response) {
            response.ContentType = "text/html";
            response.StatusCode = 200;

            // TODO: use RazorPageGenerator when ASP.NET Core 1.1 is out...?
            var builder = new StringBuilder(Template);
            builder.Replace("@{GraphQLPath}", graphqlPath);

            var data = Encoding.UTF8.GetBytes(builder.ToString());
            return response.Body.WriteAsync(data, 0, data.Length);
        }
    }
}
