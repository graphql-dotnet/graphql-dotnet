using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Execution;

/// <summary>
/// Creates a <see cref="GraphQLDocument">Document</see> representing a GraphQL AST from a plain GraphQL query string
/// </summary>
public interface IDocumentBuilder
{
    /// <summary>
    /// Specifies whether to ignore comments when parsing GraphQL document.
    /// By default, all comments are ignored.
    /// </summary>
    public bool IgnoreComments { get; set; }

    /// <summary>
    /// Specifies whether to ignore token locations when parsing GraphQL document.
    /// By default, all token locations are taken into account.
    /// </summary>
    public bool IgnoreLocations { get; set; }

    /// <summary>
    /// Maximum allowed recursion depth during parsing.
    /// Depth is calculated in terms of AST nodes.
    /// <br/>
    /// Defaults to 128 if not set.
    /// Minimum value is 1.
    /// </summary>
    public int? MaxDepth { get; set; }

    /// <summary>
    /// Parse a GraphQL request and return a <see cref="GraphQLDocument">Document</see> representing the GraphQL request AST
    /// </summary>
    GraphQLDocument Build(string body);

    /// <summary>
    /// Parse a GraphQL request using the specified parser options and return a <see cref="GraphQLDocument">Document</see> representing the GraphQL request AST
    /// </summary>
    GraphQLDocument Build(string body, ParserOptions options);
}
