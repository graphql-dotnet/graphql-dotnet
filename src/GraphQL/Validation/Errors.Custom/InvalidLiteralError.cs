using GraphQLParser.AST;

namespace GraphQL.Validation
{
    /// <summary>
    /// Represents an error triggered by an invalid literal passed within the associated document.
    /// </summary>
    public class InvalidLiteralError : ValidationError
    {
        // The specification does not contain rules for validating the actual literal values, so the number of the entire section of the specification is used.
        private const string NUMBER = "5.6";

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidLiteralError"/> class for a specified literal and error message.
        /// </summary>
        public InvalidLiteralError(GraphQLDocument document, ASTNode fieldOrFragmentSpread, GraphQLDirective? directive, GraphQLArgument argument, ASTNode node, string message)
            : base(document.Source, NUMBER, $"Invalid literal for {GetFieldDescription(fieldOrFragmentSpread)} {(directive != null ? $"directive '{directive.Name.StringValue} '" : "")}argument '{argument.Name.StringValue}'. {message}", node)
        {
            Code = "INVALID_LITERAL";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidLiteralError"/> class for a specified literal
        /// and exeption. Loads any exception data from the inner exception into this instance.
        /// </summary>
        public InvalidLiteralError(GraphQLDocument document, ASTNode fieldOrFragmentSpread, GraphQLDirective? directive, GraphQLArgument argument, ASTNode node, Exception innerException)
            : base(document.Source, NUMBER, $"Invalid literal for {GetFieldDescription(fieldOrFragmentSpread)} {(directive != null ? $"directive '{directive.Name.StringValue} '" : "")}argument '{argument.Name.StringValue}'. {innerException.Message}", innerException, node)
        {
            Code = "INVALID_LITERAL";
        }

        private static string GetFieldDescription(ASTNode node) => node switch
        {
            GraphQLField field => $"field '{field.Name.StringValue}'",
            GraphQLFragmentSpread fragmentSpread => $"fragment spread for fragment '{fragmentSpread.FragmentName.Name.StringValue}'",
            GraphQLInlineFragment => "inline fragment",
            _ => "node",
        };
    }
}
