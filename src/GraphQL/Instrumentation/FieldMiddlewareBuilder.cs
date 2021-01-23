using System;
using System.Threading.Tasks;
using GraphQL.Resolvers;

namespace GraphQL.Instrumentation
{
    /// <summary>
    /// Default implementation of <see cref="IFieldMiddlewareBuilder"/>.
    /// </summary>
    public class FieldMiddlewareBuilder : IFieldMiddlewareBuilder
    {
        private Func<FieldMiddlewareDelegate, FieldMiddlewareDelegate> _middleware;

        /// <inheritdoc/>
        public IFieldMiddlewareBuilder Use(Func<FieldMiddlewareDelegate, FieldMiddlewareDelegate> middleware)
        {
            if (middleware == null)
                throw new ArgumentNullException(nameof(middleware));

            if (_middleware == null)
            {
                _middleware = middleware;
            }
            else
            {
                var firstMiddleware = _middleware;
                _middleware = (d) => firstMiddleware(middleware(d));
            }

            return this;
        }

        /// <inheritdoc/>
        public Func<FieldMiddlewareDelegate, FieldMiddlewareDelegate> Build()
        {
            if (_middleware == null)
                return null;

            return (start) =>
            {
                start ??= (context => Task.FromResult(NameFieldResolver.Instance.Resolve(context)));

                return _middleware(start);
            };
        }
    }
}
