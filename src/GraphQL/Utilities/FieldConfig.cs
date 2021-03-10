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

        /// <summary>
        /// Gets or sets the name of the field.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the field description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the reason this field has been deprecated;
        /// <see langword="null"/> if this element has not been deprecated.
        /// Only applies to fields of output graph types.
        /// </summary>
        public string DeprecationReason { get; set; }

        /// <summary>
        /// Gets or sets the default value of the field. Only applies to fields of input object graph types.
        /// </summary>
        public object DefaultValue { get; set; }

        public IFieldResolver Resolver { get; set; }

        public IEventStreamResolver Subscriber { get; set; }

        public IAsyncEventStreamResolver AsyncSubscriber { get; set; }

        public IAccessor ResolverAccessor { get; set; }

        public IAccessor SubscriberAccessor { get; set; }
    }
}
