namespace GraphQL;

/// <summary>
/// Indicates that <see cref="GraphQLBuilderExtensions.AddAutoClrMappings(DI.IGraphQLBuilder, bool, bool)"/>
/// should include this class when creating auto CLR type mappings, even when <c>mapInputTypes</c>
/// or <c>mapOutputTypes</c> is <see langword="false"/>.
/// This attribute should be placed on the CLR type that comprises the mapping.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class MapAutoClrTypeAttribute : Attribute
{
}
