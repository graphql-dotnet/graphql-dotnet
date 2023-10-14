using GraphQLParser.AST;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.UniqueArgumentNames"/>
    [Serializable]
    public class UniqueArgumentNamesError : ValidationError
    {
        internal const string NUMBER = "5.4.2";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public UniqueArgumentNamesError(ValidationContext context, GraphQLArgument node, GraphQLArgument otherNode)
            : base(context.Document.Source, NUMBER, DuplicateArgMessage(node.Name.StringValue), node, otherNode)
        {
        }

        internal static string DuplicateArgMessage(string argName)
            => $"There can be only one argument named '{argName}'.";
    }
}
