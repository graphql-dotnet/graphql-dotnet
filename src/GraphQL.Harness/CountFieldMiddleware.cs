using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Instrumentation;
using GraphQL.StarWars;
using Microsoft.AspNetCore.Http;

namespace Example
{
    /// <summary>
    /// Example of Field Middleware with dependencies. When configured as singleton in DI
    /// counts fields execution for the entire application lifetime. When configured as
    /// scoped (with a scoped schema), counts how many fields were executed when processing the single request.
    /// </summary>
    public sealed class CountFieldMiddleware : IFieldMiddleware, IDisposable
    {
        private int _count;

        public CountFieldMiddleware(IHttpContextAccessor accessor, StarWarsData data)
        {
            // these dependencies are not needed here and are used only for demonstration purposes
            Debug.Assert(accessor != null);
            Debug.Assert(data != null);
        }

        public Task<object> Resolve(IResolveFieldContext context, FieldMiddlewareDelegate next)
        {
            Interlocked.Increment(ref _count);

            return next(context);
        }

        public void Dispose()
        {
            Console.WriteLine($"{_count} fields were executed");
        }
    }
}
