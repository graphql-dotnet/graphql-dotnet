using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation.Errors
{
    public class ScalarLeafsError : ValidationError
    {
        public const string PARAGRAPH = "5.3.3";

        public ScalarLeafsError(ValidationContext context, SelectionSet node, Field field, IGraphType type)
            : base(context.OriginalQuery, PARAGRAPH, NoSubselectionAllowedMessage(field.Name, context.Print(type)), node)
        {
        }

        public ScalarLeafsError(ValidationContext context, Field node, IGraphType type)
            : base(context.OriginalQuery, PARAGRAPH, RequiredSubselectionMessage(node.Name, context.Print(type)), node)
        {
        }

        internal static string NoSubselectionAllowedMessage(string field, string type)
            => $"Field {field} of type {type} must not have a sub selection";

        internal static string RequiredSubselectionMessage(string field, string type)
            => $"Field {field} of type {type} must have a sub selection";
    }
}
