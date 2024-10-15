using GraphQLParser;
using GraphQLParser.AST;
using GraphQLParser.Exceptions;

namespace GraphQL.Execution;

/// <summary>
/// <inheritdoc cref="IDocumentBuilder"/>
/// <br/><br/>
/// Default instance of <see cref="IDocumentBuilder"/>.
/// </summary>
public class GraphQLDocumentBuilder : IDocumentBuilder
{
    /// <inheritdoc/>
    public bool IgnoreComments { get; set; } = true;

    /// <inheritdoc/>
    public bool IgnoreLocations { get; set; }

    /// <inheritdoc/>
    public int? MaxDepth { get; set; }

    /// <inheritdoc/>
    public GraphQLDocument Build(string body)
    {
        try
        {
            return Parser.Parse(body, new ParserOptions { Ignore = CreateIgnoreOptions(), MaxDepth = MaxDepth });
        }
        catch (GraphQLSyntaxErrorException ex)
        {
            throw new SyntaxError(ex);
        }
    }

    /// <inheritdoc/>
    public GraphQLDocument Build(string body, ParserOptions options)
    {
        try
        {
            return Parser.Parse(body, options);
        }
        catch (GraphQLSyntaxErrorException ex)
        {
            throw new SyntaxError(ex);
        }
    }

    private IgnoreOptions CreateIgnoreOptions()
    {
        var options = IgnoreOptions.None;
        if (IgnoreComments)
            options |= IgnoreOptions.Comments;
        if (IgnoreLocations)
            options |= IgnoreOptions.Locations;
        return options;
    }
}
