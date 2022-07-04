using GraphQLParser.AST;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.UniqueOperationNames"/>
    [Serializable]
    public class UniqueOperationNamesError : ValidationError
    {
        internal const string NUMBER = "5.2.1.1";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public UniqueOperationNamesError(ValidationContext context, GraphQLOperationDefinition node)
            : base(context.Document.Source, NUMBER, DuplicateOperationNameMessage(node.Name!.StringValue), node)
        {
        }

        internal static string DuplicateOperationNameMessage(string opName)
            => $"There can only be one operation named {opName}.";
    }
}
