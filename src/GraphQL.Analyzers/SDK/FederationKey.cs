using GraphQL.Analyzers.Helpers;
using GraphQLParser.AST;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Location = Microsoft.CodeAnalysis.Location;

namespace GraphQL.Analyzers.SDK;

/// <summary>
/// Represents a Federation @key directive applied to a GraphQL graph type.
/// </summary>
public sealed class FederationKey
{
    private readonly Lazy<GraphQLSelectionSet?> _fields;
    private readonly Lazy<string?> _fieldsString;
    private readonly Lazy<bool> _resolvable;
    private readonly Lazy<Location> _location;
    private readonly Lazy<ExpressionSyntax?> _fieldsArgument;

    private FederationKey(
        GraphQLGraphType graphType,
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel)
    {
        GraphType = graphType;
        Syntax = invocation;
        SemanticModel = semanticModel;

        _fieldsArgument = new Lazy<ExpressionSyntax?>(() => GetArgumentValue(Syntax, SemanticModel, "fields"));
        _fieldsString = new Lazy<string?>(GetFieldsString);
        _resolvable = new Lazy<bool>(GetResolvable);
        _location = new Lazy<Location>(invocation.GetLocation);
        _fields = new Lazy<GraphQLSelectionSet?>(ParseFields);
    }

    /// <summary>
    /// Creates a FederationKey from an invocation expression, if it represents a .Key() method call.
    /// Returns null if the invocation is not a Key method or cannot be analyzed.
    /// </summary>
    public static FederationKey? TryCreate(InvocationExpressionSyntax invocation, SemanticModel semanticModel, GraphQLGraphType graphType)
    {
        // Check if this is a .Key() method call
        if (!invocation.IsGraphQLMethodInvocation(semanticModel, "Key"))
            return null;

        return new FederationKey(graphType, invocation, semanticModel);
    }

    /// <summary>
    /// Gets the underlying invocation expression syntax.
    /// </summary>
    public InvocationExpressionSyntax Syntax { get; }

    /// <summary>
    /// Gets the semantic model used for analysis.
    /// </summary>
    public SemanticModel SemanticModel { get; }

    /// <summary>
    /// Gets the parsed selection set representing the key fields.
    /// </summary>
    public GraphQLSelectionSet? Fields => _fields.Value;

    /// <summary>
    /// Gets the original fields string argument from the @key directive.
    /// </summary>
    public string? FieldsString => _fieldsString.Value;

    /// <summary>
    /// Gets whether the key is resolvable. Default is true.
    /// </summary>
    public bool Resolvable => _resolvable.Value;

    /// <summary>
    /// Gets the location of the @key directive in source code.
    /// </summary>
    public Location Location => _location.Value;

    /// <summary>
    /// Gets the graph type that this key is applied to.
    /// </summary>
    public GraphQLGraphType GraphType { get; }

    private string? GetFieldsString()
    {
        var fieldsArg = _fieldsArgument.Value;
        if (fieldsArg == null)
            return null;

        FederationFieldsHelper.TryGetFieldsString(fieldsArg, SemanticModel, out var fieldsString);
        return fieldsString;
    }

    private bool GetResolvable()
    {
        var resolvableArg = GetArgumentValue(Syntax, SemanticModel, "resolvable");
        if (resolvableArg is LiteralExpressionSyntax { Token.Value: bool boolValue })
        {
            return boolValue;
        }

        return true; // Default value
    }

    private GraphQLSelectionSet? ParseFields()
    {
        return FederationFieldsHelper.ParseFields(FieldsString);
    }

    /// <summary>
    /// Gets the source location for a specific field name within the key expression.
    /// Returns the location of the entire key expression if the specific field location cannot be determined.
    /// </summary>
    /// <param name="fieldName">The name of the field to locate.</param>
    /// <param name="graphQLPosition">
    /// The position of the field in the GraphQL string that was parsed (0-based), or -1 to find the first occurrence.
    /// This string includes the wrapping braces added by <see cref="ParseFields"/>, and the implementation adjusts the
    /// position to account for the opening brace that was added during parsing.
    /// </param>
    /// <returns>The location of the field name in the source code.</returns>
    public Location GetFieldLocation(string fieldName, int graphQLPosition = -1)
    {
        return FederationFieldsHelper.GetFieldLocation(
            _fieldsArgument.Value,
            fieldName,
            graphQLPosition,
            Syntax.SyntaxTree,
            SemanticModel,
            Location);
    }

    /// <summary>
    /// Gets the field names referenced in this key.
    /// For simple keys like "id" or "id name", returns the field names directly.
    /// For composite keys like "user { id }", returns the top-level field names.
    /// </summary>
    public IEnumerable<string> GetFieldNames()
    {
        return FederationFieldsHelper.GetFieldNames(Fields);
    }

    private static ExpressionSyntax? GetArgumentValue(InvocationExpressionSyntax invocation, SemanticModel semanticModel, string argumentName)
    {
        return invocation.GetMethodArgument(argumentName, semanticModel)?.Expression;
    }
}
