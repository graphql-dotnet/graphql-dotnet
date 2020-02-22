using GraphQL.Resolvers;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQL.Instrumentation
{
    /// <summary>
    /// Default implementation of <see cref="IFieldMiddlewareBuilder"/>.
    /// </summary>
    public class FieldMiddlewareBuilder : IFieldMiddlewareBuilder
    {
        private Func<ISchema, FieldMiddlewareDelegate, FieldMiddlewareDelegate> _singleMiddleware;
        private IList<Func<ISchema, FieldMiddlewareDelegate, FieldMiddlewareDelegate>> _middlewares;

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

        public FieldMiddlewareDelegate Build(FieldMiddlewareDelegate start = null, ISchema schema = null)
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

        private bool Empty => _singleMiddleware == null;

        public void ApplyTo(ISchema schema)
        {
            // allocation free optimization if no middlewares are defined
            if (!Empty)
            {
                foreach (var complex in schema.AllTypes.OfType<IComplexGraphType>())
                {
                    foreach (var field in complex.Fields)
                    {
                        var resolver = new MiddlewareResolver(field.Resolver);

                        var fieldMiddlewareDelegate = Build(resolver.Resolve, schema);

                        field.Resolver = new FuncFieldResolver<object>(fieldMiddlewareDelegate.Invoke);
                    }
                }
            }
        }
    }
}
