using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Instrumentation
{
    public interface IFieldMiddlewareBuilder
    {
        IFieldMiddlewareBuilder Use(Func<FieldMiddlewareDelegate, FieldMiddlewareDelegate> middleware);
        FieldMiddlewareDelegate Build(FieldMiddlewareDelegate start = null);
        void ApplyTo(ISchema schema);
    }

    public class FieldMiddlewareBuilder : IFieldMiddlewareBuilder
    {
        private readonly IList<Func<FieldMiddlewareDelegate, FieldMiddlewareDelegate>> _components = new List<Func<FieldMiddlewareDelegate, FieldMiddlewareDelegate>>();

        public IFieldMiddlewareBuilder Use(Func<FieldMiddlewareDelegate, FieldMiddlewareDelegate> middleware)
        {
            _components.Add(middleware ?? throw new ArgumentNullException(nameof(middleware)));
            return this;
        }

        public FieldMiddlewareDelegate Build(FieldMiddlewareDelegate start = null)
        {
            var app = start ?? (context => Task.FromResult(new NameFieldResolver().Resolve(context)));

            foreach (var component in _components.Reverse())
            {
                app = component(app);
            }

            return app;
        }

        public void ApplyTo(ISchema schema)
        {
            foreach (var complex in schema.AllTypes.OfType<IComplexGraphType>())
            {
                foreach (var field in complex.Fields)
                {
                    var resolver = new MiddlewareResolver(field.Resolver);

                    FieldMiddlewareDelegate app = Build(resolver.Resolve);

                    field.Resolver = new FuncFieldResolver<object>(app.Invoke);
                }
            }
        }
    }
}
