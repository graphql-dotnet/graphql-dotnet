using System;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL.Reflection;
using GraphQL.Resolvers;
using GraphQL.Subscription;
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
            config.ResolverAccessor = Type.ToAccessor(field, ResolverType.Resolver);
            config.SubscriberAccessor = Type.ToAccessor(field, ResolverType.Subscriber);

            if(Type != null)
            {
                if(config.ResolverAccessor == null)
                {
                    throw new InvalidOperationException($"Expected to find method or property {field} on {Type.Name} but could not.");
                }

                config.Resolver = new AccessorFieldResolver(config.ResolverAccessor, dependencyResolver);
                config.ResolverAccessor.GetAttributes<GraphQLAttribute>()?.Apply(a => a.Modify(config));

                if (config.SubscriberAccessor != null)
                {
                    config.Subscriber = new EventStreamResolver(config.SubscriberAccessor, dependencyResolver);
                }
            }

            return config;
        }

        private void ApplyMetadata(Type type)
        {
            var attributes = type?.GetTypeInfo().GetCustomAttributes<GraphQLAttribute>();
            attributes?.Apply(a => a.Modify(this));
        }
    }
}
