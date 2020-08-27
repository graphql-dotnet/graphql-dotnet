using System;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation.Errors
{
    [Serializable]
    public class ScalarLeafsError : ValidationError
    {
        internal const string NUMBER = "5.3.3";

        public ScalarLeafsError(ValidationContext context, SelectionSet node, Field field, IGraphType type)
            : base(context.OriginalQuery, NUMBER, NoSubselectionAllowedMessage(field.Name, context.Print(type)), node)
        {
        }

        public ScalarLeafsError(ValidationContext context, Field node, IGraphType type)
            : base(context.OriginalQuery, NUMBER, RequiredSubselectionMessage(node.Name, context.Print(type)), node)
        {
        }

        internal static string NoSubselectionAllowedMessage(string field, string type)
            => $"Field {field} of type {type} must not have a sub selection";

        internal static string RequiredSubselectionMessage(string field, string type)
            => $"Field {field} of type {type} must have a sub selection";
    }
}
