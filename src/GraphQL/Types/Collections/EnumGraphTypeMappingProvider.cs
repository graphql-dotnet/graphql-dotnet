namespace GraphQL.Types;

/// <summary>
/// Maps CLR enum types to <see cref="EnumerationGraphType{TEnum}"/>.
/// </summary>
public sealed class EnumGraphTypeMappingProvider : IGraphTypeMappingProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnumGraphTypeMappingProvider"/> class.
    /// </summary>
    [RequiresUnreferencedCode("Enumeration graph types to be created cannot be statically referenced.")]
    [RequiresDynamicCode("Creating generic enumeration types requires dynamic code.")]
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

    [UnconditionalSuppressMessage("Trimming", "IL3050: Avoid calling members annotated with 'RequiresDynamicCodeAttribute' when publishing as Native AOT",
        Justification = "The constructor is marked with RequiresDynamicCodeAttribute.")]
    [UnconditionalSuppressMessage("AOT", "IL2070:Calling members with arguments having 'DynamicallyAccessedMembersAttribute' may break functionality when trimming application code.",
        Justification = "The constructor is marked with RequiresUnreferencedCodeAttribute.")]
    private static Type CreateEnumerationGraphTypeNoWarn(Type enumType)
    {
        return typeof(EnumerationGraphType<>).MakeGenericType(enumType);
    }
}
