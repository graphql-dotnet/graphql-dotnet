using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Provides extension methods for configuring field metadata.
/// </summary>
public static class FieldExtensions
{
    /// <summary>
    /// Instructs the GraphQL input object type to bypass automatic CLR mapping for the field.
    /// </summary>
    /// <remarks>
    /// This extension method sets a specific metadata flag on the field (using the keys defined on <see cref="InputObjectGraphType"/>)
    /// to indicate that the field should not be automatically bound to a property on the corresponding CLR type.
    /// This is particularly useful when the input type defines a field that is computed or otherwise does not have a matching
    /// CLR property. In such cases, developers typically override <see cref="InputObjectGraphType{TSourceType}.ParseDictionary(IDictionary{string, object?})"/>
    /// to handle the conversion between the input and CLR object.
    /// </remarks>
    [AllowedOn<IInputObjectGraphType>]
    public static TMetadataWriter NoClrMapping<TMetadataWriter>(this TMetadataWriter fieldType)
        where TMetadataWriter : IFieldMetadataWriter
        => fieldType.WithMetadata(InputObjectGraphType.ORIGINAL_EXPRESSION_PROPERTY_NAME, InputObjectGraphType.SKIP_EXPRESSION_VALUE_NAME);

    /// <summary>
    /// Specifies that the field depends on a specific service type provided by the dependency injection provider.
    /// </summary>
    [AllowedOn<IObjectGraphType>]
    public static TMetadataWriter DependsOn<TMetadataWriter>(this TMetadataWriter fieldType, Type serviceType)
        where TMetadataWriter : IFieldMetadataWriter
    {
        var keys = fieldType.GetMetadata<List<Type>>(FromServicesAttribute.REQUIRED_SERVICES_METADATA);
        if (keys == null)
        {
            keys = [];
            fieldType.Metadata[FromServicesAttribute.REQUIRED_SERVICES_METADATA] = keys;
        }
        keys.Add(serviceType);
        return fieldType;
    }
}
