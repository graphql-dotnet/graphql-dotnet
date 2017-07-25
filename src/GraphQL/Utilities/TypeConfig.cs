using System;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    public class TypeConfig : MetadataProvider
    {
        private readonly LightweightCache<string, FieldConfig> _resolvers;

        public TypeConfig(string name)
        {
            _resolvers = new LightweightCache<string, FieldConfig>(s => new FieldConfig {Name = s});

            Name = name;
        }

        public string Name { get; }
        public string Description { get; set; }
        public Func<object, bool> IsTypeOf { get; set; }
        public Func<object, IObjectGraphType> ResolveType { get; set; }

        public IFieldResolver ResolverFor(string field)
        {
            return _resolvers[field].ResolverValue;
        }

        public void Field(string name, Action<FieldConfig> configure)
        {
            var config = _resolvers[name];
            configure(config);
        }

        public void Field<TSourceType, TReturnType>(string name, Func<ResolveFieldContext<TSourceType>, TReturnType> resolver)
        {
            var r = new FuncFieldResolver<TSourceType, TReturnType>(resolver);
            _resolvers[name].ResolverValue = r;
        }

        public void Field<TReturnType>(string name, Func<ResolveFieldContext, TReturnType> resolver)
        {
            var r = new FuncFieldResolver<TReturnType>(resolver);
            _resolvers[name].ResolverValue = r;
        }
    }
}