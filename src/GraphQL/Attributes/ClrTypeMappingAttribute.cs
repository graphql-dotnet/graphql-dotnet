namespace GraphQL;

/// <summary>
/// Specifies the CLR type mapping for a graph type when scanning an assembly via
/// <see cref="SchemaExtensions.RegisterTypeMappings(Types.ISchema)"/> or
/// <see cref="GraphQLBuilderExtensions.AddClrTypeMappings(DI.IGraphQLBuilder)"/>.
/// This attribute can be used to specify a CLR type mapping that differs from the
/// inferred type based on the generic type argument of the graph type's base class.
/// The <see cref="DoNotMapClrTypeAttribute"/> takes priority over this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class ClrTypeMappingAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClrTypeMappingAttribute"/> class.
    /// </summary>
    /// <param name="clrType">The CLR type to map to the graph type.</param>
    public ClrTypeMappingAttribute(Type clrType)
    {
        ClrType = clrType ?? throw new ArgumentNullException(nameof(clrType));
    }

    /// <summary>
    /// Gets the CLR type to map to the graph type.
    /// </summary>
    public Type ClrType { get; }
}
