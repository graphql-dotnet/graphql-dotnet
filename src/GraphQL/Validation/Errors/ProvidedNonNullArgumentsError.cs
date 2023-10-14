using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.ProvidedNonNullArguments"/>
    [Serializable]
    public class ProvidedNonNullArgumentsError : ValidationError
    {
        internal const string NUMBER = "5.4.2.1";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public ProvidedNonNullArgumentsError(ValidationContext context, GraphQLField node, QueryArgument arg)
            : base(context.Document.Source, NUMBER, MissingFieldArgMessage(node.Name.StringValue, arg.Name, arg.ResolvedType!.ToString()!), node)
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public ProvidedNonNullArgumentsError(ValidationContext context, GraphQLDirective node, QueryArgument arg)
            : base(context.Document.Source, NUMBER, MissingDirectiveArgMessage(node.Name.StringValue, arg.Name, arg.ResolvedType!.ToString()!), node)
        {
        }

        internal static string MissingFieldArgMessage(string fieldName, string argName, string type)
            => $"Argument '{argName}' of type '{type}' is required for field '{fieldName}' but not provided.";

        internal static string MissingDirectiveArgMessage(string directiveName, string argName, string type)
            => $"Argument '{argName}' of type '{type}' is required for directive '{directiveName}' but not provided.";
    }
}
