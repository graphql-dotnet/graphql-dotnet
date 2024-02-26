using GraphQLParser.AST;

namespace GraphQL.Validation;

/// <summary>
/// Represents an error triggered by an invalid literal passed within the associated document.
/// </summary>
public class InvalidLiteralError : ValidationError
{
    // The specification does not contain rules for validating the actual literal values, so the number of the entire section of the specification is used.
    private const string NUMBER = "5.6";

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidLiteralError"/> class for a specified literal and error message.
    /// Loads any exception data from the inner exception into this instance.
    /// </summary>
    public InvalidLiteralError(GraphQLDocument document, ASTNode parentNode, GraphQLDirective? directive, GraphQLArgument? argument, ASTNode node, string message, Exception? innerException = null)
        : base(document.Source, NUMBER, $"Invalid literal for {GetFieldDescription(parentNode, directive, argument)}. {message}", innerException, node)
    {
        Code = "INVALID_LITERAL";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidLiteralError"/> class for a specified literal
    /// and exception. Loads any exception data from the inner exception into this instance.
    /// </summary>
    public InvalidLiteralError(GraphQLDocument document, ASTNode parentNode, GraphQLDirective? directive, GraphQLArgument? argument, ASTNode node, Exception innerException)
        : this(document, parentNode, directive, argument, node, innerException.Message, innerException)
    {
    }

    private static string GetFieldDescription(ASTNode node, GraphQLDirective? directive, GraphQLArgument? argument)
    {
        string? str = null;
        if (argument != null)
            str += $"argument '{argument.Name.StringValue}' of ";
        if (directive != null)
            str += $"directive '{directive.Name.StringValue}' for ";
        str += node switch
        {
            GraphQLField field => $"field '{field.Name.StringValue}'",
            GraphQLFragmentSpread fragmentSpread => $"fragment spread for fragment '{fragmentSpread.FragmentName.Name.StringValue}'",
            GraphQLInlineFragment => "inline fragment",
            _ => "node",
        };

        return str;
    }
}
