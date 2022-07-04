namespace GraphQL
{
    /// <summary>
    /// Indicates that <see cref="SchemaExtensions.RegisterTypeMappings(Types.ISchema)"/> and
    /// <see cref="GraphQLBuilderExtensions.AddClrTypeMappings(DI.IGraphQLBuilder)"/>
    /// should skip this class when scanning an assembly for CLR type mappings.
    /// This attribute can be placed either on the graph type or the CLR type that comprises
    /// the mapping.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class DoNotMapClrTypeAttribute : Attribute
    {
    }
}
