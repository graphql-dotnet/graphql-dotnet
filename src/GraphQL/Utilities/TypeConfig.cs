using System.Reflection;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    /// <summary>
    /// Provides configuration for specific GraphType when building schema via <see cref="SchemaBuilder"/>.
    /// </summary>
    public class TypeConfig : MetadataProvider
    {
        private readonly LightweightCache<string, FieldConfig> _fields =
            new LightweightCache<string, FieldConfig>(f => new FieldConfig(f));

        private Type? _type;

        /// <summary>
        /// Creates an instance of <see cref="TypeConfig"/> with the specified name.
        /// </summary>
        /// <param name="name">Field argument name.</param>
        public TypeConfig(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets or sets the CLR type of the GraphQL type configured by this instance.
        /// </summary>
        public Type? Type
        {
            get => _type;
            set
            {
                _type = value;
                ApplyMetadata(value);
            }
        }

        /// <summary>
        /// Gets the name of the GraphType.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the description of the GraphType.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the reason this GraphType has been deprecated;
        /// <see langword="null"/> if this element has not been deprecated.
        /// </summary>
        public string? DeprecationReason { get; set; }

        /// <inheritdoc cref="IAbstractGraphType.ResolveType"/>
        public Func<object, IObjectGraphType>? ResolveType { get; set; }

        /// <inheritdoc cref="IObjectGraphType.IsTypeOf"/>
        public Func<object, bool>? IsTypeOfFunc { get; set; }

        /// <summary>
        /// Sets the <see cref="IsTypeOfFunc"/> property to a delegate
        /// that returns <see langword="true"/> when the object is a type
        /// that can be cast to <typeparamref name="T"/>.
        /// </summary>
        public void IsTypeOf<T>()
        {
            IsTypeOfFunc = obj => obj?.GetType().IsAssignableFrom(typeof(T)) ?? false;
        }

        /// <summary>
        /// Gets configuration for specific field of GraphType by field name.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        public FieldConfig FieldFor(string fieldName) => _fields[fieldName];

        private void ApplyMetadata(Type? type)
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
