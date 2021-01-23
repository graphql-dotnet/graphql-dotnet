using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Instrumentation
{
    /// <summary>
    /// Default implementation of <see cref="IFieldMiddlewareBuilder"/>.
    /// </summary>
    public class FieldMiddlewareBuilder : IFieldMiddlewareBuilder
    {
        private Func<ISchema, FieldMiddlewareDelegate, FieldMiddlewareDelegate> _singleMiddleware;
        private IList<Func<ISchema, FieldMiddlewareDelegate, FieldMiddlewareDelegate>> _middlewares;

        /// <inheritdoc/>
        public IFieldMiddlewareBuilder Use(Func<ISchema, FieldMiddlewareDelegate, FieldMiddlewareDelegate> middleware)
        {
            middleware = middleware ?? throw new ArgumentNullException(nameof(middleware));

            if (_singleMiddleware == null)
            {
                // allocation free optimization for single middleware (InstrumentFieldsMiddleware)
                _singleMiddleware = middleware;
            }
            else if (_middlewares == null)
            {
                _middlewares = new List<Func<ISchema, FieldMiddlewareDelegate, FieldMiddlewareDelegate>>
                {
                    _singleMiddleware,
                    middleware
                };
            }
            else
            {
                _middlewares.Add(middleware);
            }

            return this;
        }

        private FieldMiddlewareDelegate Build(FieldMiddlewareDelegate start, ISchema schema)
        {
            var middlewareDelegate = start ?? (context => Task.FromResult(NameFieldResolver.Instance.Resolve(context)));

            if (_middlewares != null)
            {
                foreach (var middleware in _middlewares.Reverse())
                {
                    middlewareDelegate = middleware(schema, middlewareDelegate);
                }
            }
            else if (_singleMiddleware != null)
            {
                middlewareDelegate = _singleMiddleware(schema, middlewareDelegate);
            }

            return middlewareDelegate;
        }

        /// <inheritdoc/>
        public Func<ISchema, FieldMiddlewareDelegate, FieldMiddlewareDelegate> Build()
        {
            if (Empty)
                return null;

            return (schema, start) => Build(start, schema);
        }

        private bool Empty => _singleMiddleware == null;
    }
}
