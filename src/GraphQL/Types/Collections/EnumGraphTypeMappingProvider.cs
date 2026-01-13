namespace GraphQL.Types;

/// <summary>
/// Maps CLR enum types to <see cref="EnumerationGraphType{TEnum}"/>.
/// </summary>
public sealed class EnumGraphTypeMappingProvider : IGraphTypeMappingProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnumGraphTypeMappingProvider"/> class.
    /// </summary>
    [RequiresDynamicCode("Creating generic types requires dynamic code.")]
    public EnumGraphTypeMappingProvider()
    {
    }

    /// <inheritdoc/>
    public Type? GetGraphTypeFromClrType(Type clrType, bool isInputType, Type? preferredType)
    {
        if (preferredType != null)
            return preferredType;

        // Auto-generate EnumerationGraphType<T> for enum types
        if (clrType.IsEnum)
            return CreateEnumerationGraphTypeNoWarn(clrType);

        return null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL3050: Avoid calling members annotated with 'RequiresDynamicCodeAttribute' when publishing as Native AOT")]
    private static Type CreateEnumerationGraphTypeNoWarn(Type enumType)
    {
        return typeof(EnumerationGraphType<>).MakeGenericType(enumType);
    }
}
