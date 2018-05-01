using GraphQL.Reflection;
using GraphQL.Resolvers;

namespace GraphQL.Utilities
{
    public class FieldConfig : MetadataProvider
    {
        public FieldConfig(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public string Description { get; set; }
        public string DeprecationReason { get; set; }
        public IFieldResolver Resolver { get; set; }
        public IEventStreamResolver Subscriber { get; set; }
        public IAsyncEventStreamResolver AsyncSubscriber { get; set; }
        public IAccessor ResolverAccessor { get; set; }
        public IAccessor SubscriberAccessor { get; set; }
    }
}
