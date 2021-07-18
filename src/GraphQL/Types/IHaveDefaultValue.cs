namespace GraphQL.Types
{
    /// <summary>
    /// Provides a default value for arguments of fields or input object graph types.
    /// </summary>
    public interface IHaveDefaultValue : IProvideResolvedType
    {
        /// <summary>
        /// Returns the default value of this argument or field.
        /// </summary>
        object? DefaultValue { get; }
    }
}
