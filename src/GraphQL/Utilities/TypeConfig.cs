using System;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL.Reflection;
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
            IsTypeOfFunc = obj => obj?.GetType().IsAssignableFrom(typeof(T)) ?? false;
        }

        public FieldConfig FieldFor(string field, IServiceProvider serviceProvider)
        {
            var config = _fields[field];
            config.ResolverAccessor = Type.ToAccessor(field, ResolverType.Resolver);

            if (Type != null && config.ResolverAccessor != null)
            {
                config.Resolver = new AccessorFieldResolver(config.ResolverAccessor, serviceProvider);
                var attrs = config.ResolverAccessor.GetAttributes<GraphQLAttribute>();
                if (attrs != null)
                {
                    foreach (var a in attrs)
                        a.Modify(config);
                }
            }

            return config;
        }

        public FieldConfig SubscriptionFieldFor(string field, IServiceProvider serviceProvider)
        {
            var config = _fields[field];
            config.ResolverAccessor = Type.ToAccessor(field, ResolverType.Resolver);
            config.SubscriberAccessor = Type.ToAccessor(field, ResolverType.Subscriber);

            if (Type != null && config.ResolverAccessor != null && config.SubscriberAccessor != null)
            {
                config.Resolver = new AccessorFieldResolver(config.ResolverAccessor, serviceProvider);
                var attrs = config.ResolverAccessor.GetAttributes<GraphQLAttribute>();
                if (attrs != null)
                {
                    foreach (var a in attrs)
                        a.Modify(config);
                }

                if (config.SubscriberAccessor.MethodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    config.AsyncSubscriber = new AsyncEventStreamResolver(config.SubscriberAccessor, serviceProvider);
                }
                else
                {
                    config.Subscriber = new EventStreamResolver(config.SubscriberAccessor, serviceProvider);
                }
            }

            return config;
        }

        private void ApplyMetadata(Type type)
        {
            var attributes = type?.GetCustomAttributes<GraphQLAttribute>();

            if (attributes == null)
                return;

            foreach (var a in attributes)
            {
                a.Modify(this);
            }
        }
    }
}
