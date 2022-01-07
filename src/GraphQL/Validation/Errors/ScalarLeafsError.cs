using System;
using GraphQL.Language.AST;
using GraphQL.Types;

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
        public ScalarLeafsError(ValidationContext context, SelectionSet node, Field field, IGraphType type)
            : base(context.Document.OriginalQuery!, NUMBER, NoSubselectionAllowedMessage(field.Name, type.ToString()), node)
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public ScalarLeafsError(ValidationContext context, Field node, IGraphType type)
            : base(context.Document.OriginalQuery!, NUMBER, RequiredSubselectionMessage(node.Name, type.ToString()), node)
        {
        }

        internal static string NoSubselectionAllowedMessage(string field, string type)
            => $"Field {field} of type {type} must not have a sub selection";

        internal static string RequiredSubselectionMessage(string field, string type)
            => $"Field {field} of type {type} must have a sub selection";
    }
}
