using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Federation.Instrumentation
{
    /// <summary>
    /// <see cref="HttpRequest"> extension methods for checking headers.
    /// </summary>
    public static class HttpRequestExtensions
    {
        private const string HEADER_NAME = "apollo-federation-include-trace";
        private const string HEADER_VALUE = "ftv1";
        /// <summary>
        /// determines if federated tracing is enabled through http headers.
        /// </summary>
        /// <param name="request"<see cref="HttpRequest"> instance</param>
        /// <returns>true if apollo-federation-include-trace is set in the header</returns>
        public static bool IsFederatedTracingEnabled(this HttpRequest request)
        {
            var headers = request?.Headers;
            if (headers !=null && headers.TryGetValue(HEADER_NAME, out var values))

            {
                string value = values.FirstOrDefault();
                return HEADER_VALUE.Equals(value, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
