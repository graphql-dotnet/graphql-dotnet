using GraphQL.Resolvers;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQL.Instrumentation
{
    public interface IFieldMiddlewareBuilder
    {
        IFieldMiddlewareBuilder Use(Func<ISchema, FieldMiddlewareDelegate, FieldMiddlewareDelegate> middleware);

        void ApplyTo(ISchema schema);
    }

    public class FieldMiddlewareBuilder : IFieldMiddlewareBuilder
    {
        private Func<ISchema, FieldMiddlewareDelegate, FieldMiddlewareDelegate> _singleComponent;
        private IList<Func<ISchema, FieldMiddlewareDelegate, FieldMiddlewareDelegate>> _components;

        public IFieldMiddlewareBuilder Use(Func<ISchema, FieldMiddlewareDelegate, FieldMiddlewareDelegate> middleware)
        {
            middleware = middleware ?? throw new ArgumentNullException(nameof(middleware));

            if (_singleComponent == null)
            {
                // allocation free optimization for single middleware (InstrumentFieldsMiddleware)
                _singleComponent = middleware;
            }
            else if (_components == null)
            {
                _components = new List<Func<ISchema, FieldMiddlewareDelegate, FieldMiddlewareDelegate>>
                {
                    _singleComponent,
                    middleware
                };
            }
            else
            {
                _components.Add(middleware);
            }

            return this;
        }

        public FieldMiddlewareDelegate Build(FieldMiddlewareDelegate start = null, ISchema schema = null)
        {
            var app = start ?? (context => Task.FromResult(NameFieldResolver.Instance.Resolve(context)));

            if (_components != null)
            {
                foreach (var component in _components.Reverse())
                {
                    app = component(schema, app);
                }
            }
            else if (_singleComponent != null)
            {
                app = _singleComponent(schema, app);
            }

            return app;
        }

        private bool Empty => _singleComponent == null;

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

                        FieldMiddlewareDelegate app = Build(resolver.Resolve, schema);

                        field.Resolver = new FuncFieldResolver<object>(app.Invoke);
                    }
                }
            }
        }
    }
}
