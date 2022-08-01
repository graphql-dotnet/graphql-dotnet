namespace GraphQL.Types
{
    /// <summary>
    /// A type that has a name within the GraphQL schema.
    /// </summary>
    public interface INamedType
    {
        /// <summary>
        /// Gets or sets a type name within the GraphQL schema. Type names are case sensitive and
        /// consist of alphanumeric characters and underscores only. Type names cannot start with
        /// a digit. For List and NonNull type modifiers returns <see langword="null"/>.
        /// </summary>
        string Name { get; set; }
    }

    /// <summary>
    /// A schema element that can have description.
    /// </summary>
    public interface IProvideDescription
    {
        /// <summary>
        /// Gets or sets the element description.
        /// </summary>
        string? Description { get; set; }
    }

    /// <summary>
    /// A schema element that can be deprecated. Now implemented by <see cref="IFieldType"/> and
    /// <see cref="EnumValueDefinition"/> but in case of fields only applicable to fields of output
    /// graph types.
    /// </summary>
    public interface IProvideDeprecationReason
    {
        /// <summary>
        /// Gets or sets the reason this element has been deprecated;
        /// <see langword="null"/> if this element has not been deprecated.
        /// </summary>
        string? DeprecationReason { get; set; }
    }

    /// <summary>
    /// Represents a graph type within the GraphQL schema.
    /// </summary>
    public interface IGraphType : IProvideMetadata, IProvideDescription, IProvideDeprecationReason, INamedType
    {
        /// <summary>
        /// Initializes the graph type.
        /// </summary>
        void Initialize(ISchema schema);
    }
}
