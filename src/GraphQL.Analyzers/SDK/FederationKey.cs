using GraphQL.Analyzers.Helpers;
using GraphQLParser;
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

    private FederationKey(
        GraphQLGraphType graphType,
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel)
    {
        GraphType = graphType;
        Syntax = invocation;
        SemanticModel = semanticModel;

        _fieldsString = new Lazy<string?>(GetFieldsString);
        _resolvable = new Lazy<bool>(GetResolvable);
        _location = new Lazy<Location>(invocation.GetLocation);
        _fields = new Lazy<GraphQLSelectionSet?>(ParseFields);
    }

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

    /// <summary>
    /// Gets the underlying invocation expression syntax.
    /// </summary>
    public InvocationExpressionSyntax Syntax { get; }

    /// <summary>
    /// Gets the semantic model used for analysis.
    /// </summary>
    public SemanticModel SemanticModel { get; }

    /// <summary>
    /// Creates a FederationKey from an invocation expression, if it represents a .Key() method call.
    /// Returns null if the invocation is not a Key method or cannot be analyzed.
    /// </summary>
    public static FederationKey? TryCreate(InvocationExpressionSyntax invocation, SemanticModel semanticModel, GraphQLGraphType graphType)
    {
        // Check if this is a .Key() method call
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess ||
            memberAccess.Name.Identifier.Text != "Key")
        {
            return null;
        }

        // Verify it's a GraphQL Federation Key method
        if (!invocation.IsGraphQLSymbol(semanticModel))
        {
            return null;
        }

        return new FederationKey(graphType, invocation, semanticModel);
    }

    private string? GetFieldsString()
    {
        var fieldsArg = GetArgumentValue(Syntax, SemanticModel, "fields");
        if (fieldsArg == null)
            return null;

        TryGetFieldsString(fieldsArg, out var fieldsString);
        return fieldsString;
    }

    private static bool TryGetFieldsString(ExpressionSyntax fieldsArg, out string? fieldsString)
    {
        fieldsString = null;

        // Handle string argument
        if (fieldsArg is LiteralExpressionSyntax { Token.Value: string str })
        {
            fieldsString = str;
            return true;
        }

        // Handle string array argument (joined with spaces)
        if (fieldsArg is ImplicitArrayCreationExpressionSyntax or ArrayCreationExpressionSyntax)
        {
            var arrayElements = GetArrayElements(fieldsArg);
            if (arrayElements is not { Count: > 0 })
            {
                return false;
            }

            var stringValues = arrayElements.Value
                .OfType<LiteralExpressionSyntax>()
                .Select(e => e.Token.Value as string)
                .Where(s => s != null)
                .ToList();

            if (stringValues.Count == arrayElements.Value.Count)
            {
                fieldsString = string.Join(" ", stringValues);
                return true;
            }
        }

        return false;
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
        if (FieldsString == null)
            return null;

        // Parse the fields string into a GraphQL selection set
        // The @key directive accepts fields in the format: "id" or "id name" or "user { id }"
        // We wrap it in curly braces to make it a valid selection set
        try
        {
            return Parser.Parse<GraphQLSelectionSet>($"{{{FieldsString}}}");
        }
        catch
        {
            // Return null if parsing fails
            return null;
        }
    }

    private static ExpressionSyntax? GetArgumentValue(InvocationExpressionSyntax invocation, SemanticModel semanticModel, string argumentName)
    {
        // Check for named arguments
        var namedArg = invocation.ArgumentList.Arguments
            .FirstOrDefault(a => a.NameColon?.Name.Identifier.Text == argumentName);

        if (namedArg != null)
            return namedArg.Expression;

        // Check for positional arguments based on method signature
        if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
            return null;

        var paramIndex = -1;
        for (int i = 0; i < methodSymbol.Parameters.Length; i++)
        {
            if (methodSymbol.Parameters[i].Name == argumentName)
            {
                paramIndex = i;
                break;
            }
        }

        if (paramIndex >= 0 && paramIndex < invocation.ArgumentList.Arguments.Count)
        {
            return invocation.ArgumentList.Arguments[paramIndex].Expression;
        }

        return null;
    }

    private static SeparatedSyntaxList<ExpressionSyntax>? GetArrayElements(ExpressionSyntax expression)
    {
        if (expression is ImplicitArrayCreationExpressionSyntax implicitArray)
        {
            return implicitArray.Initializer.Expressions;
        }

        if (expression is ArrayCreationExpressionSyntax { Initializer: not null } arrayCreation)
        {
            return arrayCreation.Initializer.Expressions;
        }

        return null;
    }

    /// <summary>
    /// Gets the field names referenced in this key.
    /// For simple keys like "id" or "id name", returns the field names directly.
    /// For composite keys like "user { id }", returns the top-level field names.
    /// </summary>
    public IEnumerable<string> GetFieldNames()
    {
        if (Fields == null)
            yield break;

        foreach (var selection in Fields.Selections)
        {
            if (selection is GraphQLField field)
            {
                yield return field.Name.StringValue;
            }
        }
    }

    /// <summary>
    /// Checks if this key includes the specified field name at the top level.
    /// </summary>
    public bool IncludesField(string fieldName)
    {
        return GetFieldNames().Any(name => string.Equals(name, fieldName, StringComparison.Ordinal));
    }
}
