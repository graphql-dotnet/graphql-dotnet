using System;
using System.Reflection;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    public class TypeConfig : MetadataProvider
    {
        private readonly LightweightCache<string, FieldConfig> _fields =
            new LightweightCache<string, FieldConfig>(f => new FieldConfig(f));

        private Type _type;

        public TypeConfig(string name)
        {
            Name = name;
        }

        public Type Type
        {
            get => _type;
            set
            {
                _type = value;
                ApplyMetadata(value);
            }
        }

        public string Name { get; }
        public string Description { get; set; }
        public string DeprecationReason { get; set; }
        public Func<object, IObjectGraphType> ResolveType { get; set; }
        public Func<object, bool> IsTypeOfFunc { get; set; }

        public void IsTypeOf<T>()
        {
            IsTypeOfFunc = obj => obj?.GetType() == typeof(T);
        }

        public FieldConfig FieldFor(string field, IDependencyResolver dependencyResolver)
        {
            var config = _fields[field];
            config.Resolver = ResolverFor(field, dependencyResolver);
            config.MethodInfo = Type?.MethodForField(field);

            var attributes = config.MethodInfo?.GetCustomAttributes<GraphQLAttribute>();
            attributes?.Apply(a => a.Modify(config));

            return config;
        }

        private IFieldResolver ResolverFor(string field, IDependencyResolver dependencyResolver)
        {
            if (Type == null)
            {
                return null;
            }

            var method = Type.MethodForField(field);

            var resolverType = typeof(MethodModelBinderResolver<>).MakeGenericType(Type);

            var args = new object[] { method, dependencyResolver };
            var resolver = (IFieldResolver) Activator.CreateInstance(resolverType, args);

            return resolver;
        }

        private void ApplyMetadata(Type type)
        {
            var attributes = type?.GetTypeInfo().GetCustomAttributes<GraphQLAttribute>();
            attributes?.Apply(a => a.Modify(this));
        }
    }
}