using GraphQLParser;
using GraphQLParser.AST;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Location = Microsoft.CodeAnalysis.Location;

namespace GraphQL.Analyzers.SDK;

/// <summary>
/// Provides helper methods for parsing and analyzing Federation directive field arguments.
/// </summary>
public static class FederationFieldsHelper
{
    /// <summary>
    /// Attempts to extract a fields string from various expression types (string literals, constants, arrays, etc.).
    /// </summary>
    /// <param name="fieldsArg">The expression syntax representing the fields argument.</param>
    /// <param name="semanticModel">The semantic model for symbol resolution.</param>
    /// <param name="fieldsString">The extracted fields string if successful.</param>
    /// <returns>True if the fields string was successfully extracted; otherwise, false.</returns>
    public static bool TryGetFieldsString(ExpressionSyntax fieldsArg, SemanticModel semanticModel, [NotNullWhen(true)] out string? fieldsString)
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

    /// <summary>
    /// Parses a fields string into a GraphQL selection set.
    /// </summary>
    /// <param name="fieldsString">The fields string to parse.</param>
    /// <returns>The parsed GraphQL selection set, or null if parsing fails.</returns>
    public static GraphQLSelectionSet? ParseFields(string? fieldsString)
    {
        if (fieldsString == null)
            return null;

        // Parse the fields string into a GraphQL selection set
        // Federation directives accept fields in the format: "id" or "id name" or "user { id }"
        // We wrap it in curly braces to make it a valid selection set
        try
        {
            return Parser.Parse<GraphQLSelectionSet>($"{{{fieldsString}}}");
        }
        catch
        {
            // Return null if parsing fails
            return null;
        }
    }

    /// <summary>
    /// Gets the field names from a GraphQL selection set.
    /// For simple selections like "id" or "id name", returns the field names directly.
    /// For composite selections like "user { id }", returns the top-level field names.
    /// </summary>
    /// <param name="fields">The GraphQL selection set to extract field names from.</param>
    /// <returns>An enumerable of field names.</returns>
    public static IEnumerable<string> GetFieldNames(GraphQLSelectionSet? fields)
    {
        if (fields == null)
            yield break;

        foreach (var selection in fields.Selections)
        {
            if (selection is GraphQLField field)
                yield return field.Name.StringValue;
        }
    }

    /// <summary>
    /// Gets the source location for a specific field name within a fields expression.
    /// </summary>
    /// <param name="fieldsArg">The expression containing the fields.</param>
    /// <param name="fieldName">The name of the field to locate.</param>
    /// <param name="graphQLPosition">The position hint from the GraphQL parser (0-based), or -1 to find the first occurrence.</param>
    /// <param name="syntaxTree">The syntax tree for creating locations.</param>
    /// <param name="semanticModel">The semantic model for symbol resolution.</param>
    /// <param name="fallbackLocation">The location to return if the specific field cannot be located.</param>
    /// <returns>The location of the field name in the source code.</returns>
    public static Location GetFieldLocation(
        ExpressionSyntax? fieldsArg,
        string fieldName,
        int graphQLPosition,
        SyntaxTree syntaxTree,
        SemanticModel semanticModel,
        Location fallbackLocation)
    {
        if (fieldsArg == null)
            return fallbackLocation;

        return GetFieldLocationCore(fieldsArg, fieldName, graphQLPosition, syntaxTree, semanticModel) ?? fallbackLocation;
    }

    private static Location? GetFieldLocationCore(
        ExpressionSyntax fieldsArg,
        string fieldName,
        int graphQLPosition,
        SyntaxTree syntaxTree,
        SemanticModel semanticModel)
    {
        switch (fieldsArg)
        {
            // Handle const field/property references
            case IdentifierNameSyntax or MemberAccessExpressionSyntax when TryHandleConstant(fieldsArg, fieldName, semanticModel, out var location):
                return location;
            // Handle string literal: "id" or "id name"
            case LiteralExpressionSyntax { Token.Value: string } literalExpression when TryHandleLiteral(literalExpression, fieldName, graphQLPosition, syntaxTree, out var location):
                return location;
            // Handle nameof expressions
            case InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "nameof" } } nameofInvocation when TryHandleNameof(nameofInvocation, fieldName, out var location):
                return location;
            // Handle interpolated strings
            case InterpolatedStringExpressionSyntax interpolatedString when TryHandleInterpolatedString(interpolatedString, fieldName, graphQLPosition, syntaxTree, semanticModel, out var location):
                return location;
            // Handle collection expression syntax: ["id", "name"]
            case CollectionExpressionSyntax collectionExpression:
            {
                var expressions = collectionExpression.Elements.OfType<ExpressionElementSyntax>().Select(e => e.Expression);
                if (TryHandleCollection(expressions, fieldName, graphQLPosition, syntaxTree, semanticModel, out var location))
                    return location;

                break;
            }
            // Handle implicit array creation: new[] { "id", "name" }
            case ImplicitArrayCreationExpressionSyntax implicitArray:
            {
                if (TryHandleCollection(implicitArray.Initializer.Expressions, fieldName, graphQLPosition, syntaxTree, semanticModel, out var location))
                    return location;

                break;
            }
            // Handle explicit array creation: new string[] { "id", "name" }
            case ArrayCreationExpressionSyntax { Initializer: not null } arrayCreation:
            {
                if (TryHandleCollection(arrayCreation.Initializer.Expressions, fieldName, graphQLPosition, syntaxTree, semanticModel, out var location))
                    return location;

                break;
            }
        }

        return null;
    }

    private static bool TryHandleConstant(
        ExpressionSyntax expression,
        string fieldName,
        SemanticModel semanticModel,
        [NotNullWhen(true)] out Location? location)
    {
        var symbol = semanticModel.GetSymbolInfo(expression).Symbol;
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

    private static bool TryHandleLiteral(
        LiteralExpressionSyntax expression,
        string fieldName,
        int graphQLPosition,
        SyntaxTree syntaxTree,
        [NotNullWhen(true)] out Location? location)
    {
        var token = expression.Token;
        var literalValue = token.ValueText;

        // Find the field name within the string literal; when the same field name appears multiple
        // times in the expression, the GraphQL position hint is used to select the occurrence
        // whose position is closest to the specified GraphQL location.
        var fieldIndex = FindFieldInString(literalValue, fieldName, graphQLPosition - 1);
        if (fieldIndex >= 0)
        {
            // Calculate position within the string literal (accounting for opening quote)
            var startPosition = token.SpanStart + 1 + fieldIndex; // +1 for opening quote
            var span = new Microsoft.CodeAnalysis.Text.TextSpan(startPosition, fieldName.Length);
            location = Location.Create(syntaxTree, span);
            return true;
        }

        location = null;
        return false;
    }

    private static bool TryHandleNameof(
        InvocationExpressionSyntax expression,
        string fieldName,
        [NotNullWhen(true)] out Location? location)
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

    private static bool TryHandleInterpolatedString(
        InterpolatedStringExpressionSyntax expression,
        string fieldName,
        int graphQLPosition,
        SyntaxTree syntaxTree,
        SemanticModel semanticModel,
        [NotNullWhen(true)] out Location? location)
    {
        foreach (var content in expression.Contents)
        {
            switch (content)
            {
                // Check for direct text match
                case InterpolatedStringTextSyntax textSyntax when string.Equals(textSyntax.TextToken.ValueText, fieldName, StringComparison.OrdinalIgnoreCase):
                    location = textSyntax.GetLocation();
                    return true;
                // Check for field within text. Required when a literal located between string interpolations: $"{nameof(A)} B {C}}"
                case InterpolatedStringTextSyntax textSyntax:
                    var fieldIndex = FindFieldInString(textSyntax.TextToken.ValueText, fieldName, graphQLPosition);
                    if (fieldIndex >= 0)
                    {
                        // Calculate position within the string
                        var startPosition = textSyntax.TextToken.SpanStart + fieldIndex;
                        var span = new Microsoft.CodeAnalysis.Text.TextSpan(startPosition, fieldName.Length);
                        location = Location.Create(syntaxTree, span);
                        return true;
                    }
                    break;
                case InterpolationSyntax interpolation:
                    location = GetFieldLocationCore(interpolation.Expression, fieldName, graphQLPosition, syntaxTree, semanticModel);
                    if (location != null)
                        return true;

                    break;
            }
        }

        location = null;
        return false;
    }

    private static bool TryHandleCollection(
        IEnumerable<ExpressionSyntax> elements,
        string fieldName,
        int graphQLPosition,
        SyntaxTree syntaxTree,
        SemanticModel semanticModel,
        [NotNullWhen(true)] out Location? location)
    {
        foreach (var element in elements)
        {
            location = GetFieldLocationCore(element, fieldName, graphQLPosition, syntaxTree, semanticModel);
            if (location != null)
                return true;
        }

        location = null;
        return false;
    }

    /// <summary>
    /// Finds the index of a field name within a string, considering GraphQL structural characters.
    /// </summary>
    /// <param name="literalValue">The string to search within.</param>
    /// <param name="fieldName">The field name to find.</param>
    /// <param name="graphQLPosition">The position hint from the GraphQL parser, or -1 for first occurrence.</param>
    /// <returns>The index of the field name, or -1 if not found.</returns>
    public static int FindFieldInString(string literalValue, string fieldName, int graphQLPosition)
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
}
