using GraphQLParser.AST;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.UniqueFragmentNames"/>
    [Serializable]
    public class UniqueFragmentNamesError : ValidationError
    {
        internal const string NUMBER = "5.5.1.1";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public UniqueFragmentNamesError(ValidationContext context, GraphQLFragmentDefinition node, GraphQLFragmentDefinition altNode)
            : base(context.Document.Source, NUMBER, DuplicateFragmentNameMessage(node.FragmentName.Name.StringValue), node, altNode)
        {
        }

        internal static string DuplicateFragmentNameMessage(string fragName)
            => $"There can only be one fragment named '{fragName}'";
    }
}
