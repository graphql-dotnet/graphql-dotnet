namespace GraphQL.Types
{
    /// <summary>
    /// Represents a field of a graph type.
    /// </summary>
    public interface IFieldType : IHaveDefaultValue, IProvideMetadata, IProvideDescription, IProvideDeprecationReason
    {
        /// <summary>
        /// Gets or sets the name of the field.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets a list of arguments for the field.
        /// </summary>
        QueryArguments? Arguments { get; set; }
    }
}
