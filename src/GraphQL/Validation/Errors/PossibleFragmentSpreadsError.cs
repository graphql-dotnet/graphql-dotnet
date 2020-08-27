using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation.Errors
{
    public class PossibleFragmentSpreadsError : ValidationError
    {
        internal const string NUMBER = "5.5.2.3";

        public PossibleFragmentSpreadsError(ValidationContext context, InlineFragment node, IGraphType parentType, IGraphType fragType)
            : base(context.OriginalQuery, NUMBER, TypeIncompatibleAnonSpreadMessage(context.Print(parentType), context.Print(fragType)), node)
        {
        }

        public PossibleFragmentSpreadsError(ValidationContext context, FragmentSpread node, IGraphType parentType, IGraphType fragType)
            : base(context.OriginalQuery, NUMBER, TypeIncompatibleSpreadMessage(node.Name, context.Print(parentType), context.Print(fragType)), node)
        {
        }

        internal static string TypeIncompatibleSpreadMessage(string fragName, string parentType, string fragType)
            => $"Fragment \"{fragName}\" cannot be spread here as objects of type \"{parentType}\" can never be of type \"{fragType}\".";

        internal static string TypeIncompatibleAnonSpreadMessage(string parentType, string fragType)
            => $"Fragment cannot be spread here as objects of type \"{parentType}\" can never be of type \"{fragType}\".";
    }
}
