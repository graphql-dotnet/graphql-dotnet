using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Errors.Custom;

/// <summary>
/// Represents an error indiciating that introspection queries are not allowed.
/// </summary>
public class NoIntrospectionError : ValidationError
{
    /// <inheritdoc cref="NoIntrospectionError"/>
    public NoIntrospectionError(ROM source, ASTNode node) : base(
        source,
        null,
        "Introspection queries are not allowed.",
        node)
    {
    }
}
