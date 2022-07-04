using GraphQL.Reflection;
using GraphQL.Resolvers;

namespace GraphQL.Utilities
{
    /// <summary>
    /// Provides configuration for specific field of GraphType when building schema via <see cref="SchemaBuilder"/>.
    /// </summary>
    public class FieldConfig : MetadataProvider
    {
        private readonly LightweightCache<string, ArgumentConfig> _arguments =
           new LightweightCache<string, ArgumentConfig>(f => new ArgumentConfig(f));

        /// <summary>
        /// Creates an instance of <see cref="FieldConfig"/> with the specified name.
        /// </summary>
        /// <param name="name">Field argument name.</param>
        public FieldConfig(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the name of the field.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the field description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the reason this field has been deprecated;
        /// <see langword="null"/> if this element has not been deprecated.
        /// Only applies to fields of output graph types.
        /// </summary>
        public string? DeprecationReason { get; set; }

        /// <summary>
        /// Gets or sets the default value of the field. Only applies to fields of input object graph types.
        /// </summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets the field resolver.
        /// </summary>
        public IFieldResolver? Resolver { get; set; }

        /// <summary>
        /// Gets or sets the event stream resolver.
        /// </summary>
        public ISourceStreamResolver? StreamResolver { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IAccessor"/> representing the class member
        /// to be executed for the field resolver.
        /// </summary>
        public IAccessor? ResolverAccessor { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IAccessor"/> representing the class member
        /// to be executed for the subscription field's event stream resolver.
        /// </summary>
        public IAccessor? StreamResolverAccessor { get; set; }

        /// <summary>
        /// Gets configuration for specific field argument by argument name.
        /// </summary>
        /// <param name="argumentName">Name of the field argument.</param>
        public ArgumentConfig ArgumentFor(string argumentName) => _arguments[argumentName];
    }
}
