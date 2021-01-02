namespace GraphQL.Types
{
    /// <summary>
    /// Represents a field of a graph type.
    /// </summary>
    public interface IFieldType : IHaveDefaultValue, IProvideMetadata
    {
        /// <summary>
        /// Gets or sets the name of the field.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the field.
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// Gets or sets the deprecation reason of the field. Only applicable to fields of output graph types.
        /// </summary>
        string DeprecationReason { get; set; }

        /// <summary>
        /// Gets or sets a list of arguments for the field.
        /// </summary>
        QueryArguments Arguments { get; set; }
    }
}
