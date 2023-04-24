namespace GraphQL.Types
{
    /// <summary>
    /// Provides basic capabilities for getting and setting arbitrary meta information.
    /// This interface is implemented by numerous descendants like <see cref="GraphType"/>,
    /// <see cref="FieldType"/>, <see cref="Schema"/> or others.
    /// </summary>
    public interface IProvideMetadata : IMetadataBuilder
    {
    }
}
