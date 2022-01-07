using System;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.PossibleFragmentSpreads"/>
    [Serializable]
    public class PossibleFragmentSpreadsError : ValidationError
    {
        internal const string NUMBER = "5.5.2.3";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public PossibleFragmentSpreadsError(ValidationContext context, InlineFragment node, IGraphType parentType, IGraphType fragType)
            : base(context.Document.OriginalQuery!, NUMBER, TypeIncompatibleAnonSpreadMessage(parentType.ToString(), fragType.ToString()), node)
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public PossibleFragmentSpreadsError(ValidationContext context, FragmentSpread node, IGraphType parentType, IGraphType fragType)
            : base(context.Document.OriginalQuery!, NUMBER, TypeIncompatibleSpreadMessage(node.Name, parentType.ToString(), fragType.ToString()), node)
        {
        }

        internal static string TypeIncompatibleSpreadMessage(string fragName, string parentType, string fragType)
            => $"Fragment '{fragName}' cannot be spread here as objects of type '{parentType}' can never be of type '{fragType}'.";

        internal static string TypeIncompatibleAnonSpreadMessage(string parentType, string fragType)
            => $"Fragment cannot be spread here as objects of type '{parentType}' can never be of type '{fragType}'.";
    }
}
