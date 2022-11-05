using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.ScalarLeafs"/>
    [Serializable]
    public class ScalarLeafsError : ValidationError
    {
        internal const string NUMBER = "5.3.3";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public ScalarLeafsError(ValidationContext context, GraphQLSelectionSet node, GraphQLField field, IGraphType type)
            : base(context.Document.Source, NUMBER, NoSubselectionAllowedMessage(field.Name.StringValue, type.ToString()!), node)
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public ScalarLeafsError(ValidationContext context, GraphQLField node, IGraphType type)
            : base(context.Document.Source, NUMBER, RequiredSubselectionMessage(node.Name.StringValue, type.ToString()!), node)
        {
        }

        internal static string NoSubselectionAllowedMessage(string field, string type)
            => $"Field {field} of type {type} must not have a sub selection";

        internal static string RequiredSubselectionMessage(string field, string type)
            => $"Field {field} of type {type} must have a sub selection";
    }
}
