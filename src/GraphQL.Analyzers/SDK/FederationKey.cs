using GraphQL.Analyzers.Helpers;
using GraphQLParser;
using GraphQLParser.AST;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

        TryGetFieldsString(fieldsArg, SemanticModel, out var fieldsString);
        return fieldsString;
    }

    private static bool TryGetFieldsString(ExpressionSyntax fieldsArg, SemanticModel semanticModel, [NotNullWhen(true)] out string? fieldsString)
    {
        fieldsString = null;

        // Handle string argument
        if (fieldsArg is LiteralExpressionSyntax { Token.Value: string str })
        {
            fieldsString = str;
            return true;
        }

        // Handle const field/property references
        if (fieldsArg is IdentifierNameSyntax or MemberAccessExpressionSyntax)
        {
            var symbol = semanticModel.GetSymbolInfo(fieldsArg).Symbol;
            if (symbol is IFieldSymbol { IsConst: true, ConstantValue: string constValue })
            {
                fieldsString = constValue;
                return true;
            }
        }

        // Handle nameof expressions
        if (fieldsArg is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "nameof" } } nameofInvocation)
        {
            var nameofValue = nameofInvocation.ArgumentList.Arguments[0].Expression
                .DescendantNodesAndSelf()
                .OfType<IdentifierNameSyntax>()
                .LastOrDefault()
                ?.Identifier
                .ValueText;

            if (nameofValue != null)
            {
                fieldsString = nameofValue;
                return true;
            }
        }

        // Handle interpolated strings
        if (fieldsArg is InterpolatedStringExpressionSyntax interpolatedString)
        {
            var parts = new List<string>();
            foreach (var content in interpolatedString.Contents)
            {
                switch (content)
                {
                    case InterpolatedStringTextSyntax textSyntax:
                        parts.Add(textSyntax.TextToken.ValueText);
                        break;
                    // Try to extract value from interpolation expression
                    case InterpolationSyntax interpolation when TryGetFieldsString(interpolation.Expression, semanticModel, out var interpolatedValue):
                        parts.Add(interpolatedValue);
                        break;
                    default:
                        return false;
                }
            }

            fieldsString = string.Concat(parts);
            return true;
        }

        // Handle string array argument (joined with spaces)
        var arrayElements = fieldsArg switch
        {
            ImplicitArrayCreationExpressionSyntax implicitArray =>
                implicitArray.Initializer.Expressions.ToArray(),
            ArrayCreationExpressionSyntax { Initializer: not null } arrayCreation =>
                arrayCreation.Initializer.Expressions.ToArray(),
            CollectionExpressionSyntax collectionExpression =>
                collectionExpression.Elements
                    .OfType<ExpressionElementSyntax>()
                    .Select(e => e.Expression)
                    .ToArray(),
            _ => null
        };

        if (arrayElements == null || arrayElements.Length == 0)
        {
            return false;
        }
        var stringValues = new List<string>(arrayElements.Length);

        foreach (var element in arrayElements)
        {
            if (TryGetFieldsString(element, semanticModel, out var elementString))
            {
                stringValues.Add(elementString);
            }
        }

        if (stringValues.Count == arrayElements.Length)
        {
            fieldsString = string.Join(" ", stringValues);
            return true;
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

    /// <summary>
    /// Gets the source location for a specific field name within the key expression.
    /// Returns the location of the entire key expression if the specific field location cannot be determined.
    /// </summary>
    /// <param name="fieldName">The name of the field to locate.</param>
    /// <param name="graphQLPosition">The position of the field in the parsed GraphQL string (0-based), or -1 to find the first occurrence.</param>
    /// <returns>The location of the field name in the source code.</returns>
    public Location GetFieldLocation(string fieldName, int graphQLPosition = -1)
    {
        var fieldsArg = _fieldsArgument.Value;
        if (fieldsArg == null)
        {
            return Location;
        }

        // Fall back to the entire key expression
        return GetFieldLocation(fieldsArg, fieldName, graphQLPosition) ?? Location;
    }

    private Location? GetFieldLocation(ExpressionSyntax fieldsArg, string fieldName, int graphQLPosition)
    {
        switch (fieldsArg)
        {
            // Handle const field/property references
            case IdentifierNameSyntax or MemberAccessExpressionSyntax when TryHandleConstant(fieldsArg, out var location):
                return location;
            // Handle string literal: "id" or "id name"
            case LiteralExpressionSyntax { Token.Value: string } literalExpression when TryHandleLiteral(literalExpression, out var location):
                return location;
            // Handle nameof expressions
            case InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "nameof" } } nameofInvocation when TryHandleNameof(nameofInvocation, out var location):
                return location;
            // Handle interpolated strings
            case InterpolatedStringExpressionSyntax interpolatedString when TryHandleInterpolatedString(interpolatedString, out var location):
                return location;
            // Handle collection expression syntax: ["id", "name"]
            case CollectionExpressionSyntax collectionExpression:
            {
                var expressions = collectionExpression.Elements.OfType<ExpressionElementSyntax>().Select(e => e.Expression);
                if (TryHandleCollection(expressions, out var location))
                    return location;

                break;
            }
            // Handle implicit array creation: new[] { "id", "name" }
            case ImplicitArrayCreationExpressionSyntax implicitArray:
            {
                if (TryHandleCollection(implicitArray.Initializer.Expressions, out var location))
                    return location;

                break;
            }
            // Handle explicit array creation: new string[] { "id", "name" }
            case ArrayCreationExpressionSyntax { Initializer: not null } arrayCreation:
            {
                if (TryHandleCollection(arrayCreation.Initializer.Expressions, out var location))
                    return location;

                break;
            }
        }

        return null;

        bool TryHandleConstant(ExpressionSyntax expression, [NotNullWhen(true)] out Location? location)
        {
            var symbol = SemanticModel.GetSymbolInfo(expression).Symbol;
            if (symbol is IFieldSymbol { IsConst: true, ConstantValue: string constValue })
            {
                if (string.Equals(constValue, fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    location = expression.GetLocation();
                    return true;
                }
            }

            location = null;
            return false;
        }

        bool TryHandleLiteral(LiteralExpressionSyntax expression, [NotNullWhen(true)] out Location? location)
        {
            var token = expression.Token;
            var literalValue = token.ValueText;

            // Find the field name within the string literal; when the same field name appears multiple
            // times in the key expression, the GraphQL position hint is used to select the occurrence
            // whose position is closest to the specified GraphQL location.
            var fieldIndex = FindFieldInString(literalValue, fieldName, graphQLPosition);
            if (fieldIndex >= 0)
            {
                // Calculate position within the string literal (accounting for opening quote)
                var startPosition = token.SpanStart + 1 + fieldIndex; // +1 for opening quote
                var span = new Microsoft.CodeAnalysis.Text.TextSpan(startPosition, fieldName.Length);
                location = Location.Create(Syntax.SyntaxTree, span);
                return true;
            }

            location = null;
            return false;
        }

        bool TryHandleNameof(InvocationExpressionSyntax expression, [NotNullWhen(true)] out Location? location)
        {
            var nameofIdentifier = expression.ArgumentList.Arguments[0].Expression
                .DescendantNodesAndSelf()
                .OfType<IdentifierNameSyntax>()
                .LastOrDefault()
                ?.Identifier;

            if (string.Equals(nameofIdentifier?.ToString(), fieldName, StringComparison.Ordinal))
            {
                location = expression.GetLocation();
                return true;
            }

            location = null;
            return false;
        }

        bool TryHandleInterpolatedString(InterpolatedStringExpressionSyntax expression, [NotNullWhen(true)] out Location? location)
        {
            foreach (var content in expression.Contents)
            {
                switch (content)
                {
                    // Check for direct text match
                    case InterpolatedStringTextSyntax textSyntax when string.Equals(textSyntax.TextToken.ValueText, fieldName, StringComparison.OrdinalIgnoreCase):
                        location = textSyntax.GetLocation();
                        return true;
                    // Check for trimmed text match. Required when a literal located between string interpolations: $"{nameof(A)} B {C}}"
                    case InterpolatedStringTextSyntax textSyntax:
                        var fieldIndex = FindFieldInString(textSyntax.TextToken.ValueText, fieldName, graphQLPosition);
                        if (fieldIndex >= 0)
                        {
                            // Calculate position within the string
                            var startPosition = textSyntax.TextToken.SpanStart + fieldIndex;
                            var span = new Microsoft.CodeAnalysis.Text.TextSpan(startPosition, fieldName.Length);
                            location = Location.Create(Syntax.SyntaxTree, span);
                            return true;
                        }
                        break;
                    case InterpolationSyntax interpolation:
                        location = GetFieldLocation(interpolation.Expression, fieldName, graphQLPosition);
                        if (location != null)
                            return true;

                        break;
                }
            }

            location = null;
            return false;
        }

        bool TryHandleCollection(IEnumerable<ExpressionSyntax> elements, [NotNullWhen(true)] out Location? location)
        {
            foreach (var element in elements)
            {
                location = GetFieldLocation(element, fieldName, graphQLPosition);
                if (location != null)
                    return true;
            }

            location = null;
            return false;
        }
    }

    private static int FindFieldInString(string literalValue, string fieldName, int graphQLPosition)
    {
        // fast track
        if (string.Equals(literalValue, fieldName, StringComparison.OrdinalIgnoreCase))
            return 0;

        int currentPosition = 0;
        int previousMatch = -1;

        while (currentPosition < literalValue.Length)
        {
            // Skip whitespace and structural characters
            while (currentPosition < literalValue.Length &&
                   (char.IsWhiteSpace(literalValue[currentPosition]) ||
                    literalValue[currentPosition] == '{' ||
                    literalValue[currentPosition] == '}'))
            {
                currentPosition++;
            }

            if (currentPosition >= literalValue.Length)
                break;

            // Find the end of the current word
            int wordStart = currentPosition;
            while (currentPosition < literalValue.Length &&
                   !char.IsWhiteSpace(literalValue[currentPosition]) &&
                   literalValue[currentPosition] != '{' &&
                   literalValue[currentPosition] != '}')
            {
                currentPosition++;
            }

            int wordLength = currentPosition - wordStart;

            // Check if this word matches the field name
            if (wordLength == fieldName.Length &&
                literalValue.AsSpan(wordStart, wordLength).Equals(fieldName.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                // No position hint - return first match
                if (graphQLPosition < 0)
                    return wordStart;

                // If this match is at or past the target position
                if (wordStart >= graphQLPosition)
                {
                    // Compare with previous match if we have one
                    if (previousMatch >= 0)
                    {
                        // Return whichever is closer to the target
                        return (graphQLPosition - previousMatch) <= (wordStart - graphQLPosition)
                            ? previousMatch
                            : wordStart;
                    }
                    return wordStart;
                }

                // This match is before the target, remember it and continue
                previousMatch = wordStart;
            }
        }

        // Only matches before target were found, return the last one
        return previousMatch;
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
                yield return field.Name.StringValue;
        }
    }

    private static ExpressionSyntax? GetArgumentValue(InvocationExpressionSyntax invocation, SemanticModel semanticModel, string argumentName)
    {
        return invocation.GetMethodArgument(argumentName, semanticModel)?.Expression;
    }
}
