using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Tools
{
    public class MetadataProvider : IProvideMetadata
    {
        public IDictionary<string, object> Metadata { get; set; } = new ConcurrentDictionary<string, object>();

        public TType GetMetadata<TType>(string key, TType defaultValue = default(TType))
        {
            if (!HasMetadata(key))
            {
                return defaultValue;
            }

            object item;
            if (Metadata.TryGetValue(key, out item))
            {
                return (TType) item;
            }

            return defaultValue;
        }

        public bool HasMetadata(string key)
        {
            return Metadata.ContainsKey(key);
        }
    }

    public class FieldConfig : MetadataProvider
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public IFieldResolver ResolverValue { get; set; }

        public void Resolver<TSourceType, TReturnType>(Func<ResolveFieldContext<TSourceType>, TReturnType> resolver)
        {
            ResolverValue = new FuncFieldResolver<TSourceType, TReturnType>(resolver);
        }

        public void Resolver<TReturnType>(Func<ResolveFieldContext, TReturnType> resolver)
        {
            ResolverValue = new FuncFieldResolver<TReturnType>(resolver);
        }
    }

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