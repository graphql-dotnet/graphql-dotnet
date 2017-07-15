using System;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Tools
{
    public class TypeConfig
    {
        private readonly LightweightCache<string, IFieldResolver> _resolvers;

        public TypeConfig(string name)
        {
            _resolvers = new LightweightCache<string, IFieldResolver>(s => null);

            Name = name;
        }

        public string Name { get; }

        public Func<object, bool> IsTypeOf { get; set; }

        public IFieldResolver ResolverFor(string field)
        {
            return _resolvers[field];
        }

        public void Resolver<TSourceType, TReturnType>(string field, Func<ResolveFieldContext<TSourceType>, TReturnType> resolver)
        {
            Resolver(field, new FuncFieldResolver<TSourceType, TReturnType>(resolver));
        }

        public void Resolver<TReturnType>(string field, Func<ResolveFieldContext, TReturnType> resolver)
        {
            Resolver(field, new FuncFieldResolver<TReturnType>(resolver));
        }

        public void Resolver(string field, IFieldResolver resolver)
        {
            _resolvers[field] = resolver;
        }
    }
}