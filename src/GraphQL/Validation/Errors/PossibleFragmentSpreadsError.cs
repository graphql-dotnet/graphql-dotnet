using System;
using GraphQL.Types;
using GraphQLParser.AST;

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
        public PossibleFragmentSpreadsError(ValidationContext context, GraphQLInlineFragment node, IGraphType parentType, IGraphType fragType)
            : base(context.Document.Source, NUMBER, TypeIncompatibleAnonSpreadMessage(parentType, fragType), node)
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public PossibleFragmentSpreadsError(ValidationContext context, GraphQLFragmentSpread node, IGraphType parentType, IGraphType fragType)
            : base(context.Document.Source, NUMBER, TypeIncompatibleSpreadMessage(node.FragmentName.Name.StringValue, parentType, fragType), node)
        {
        }

        internal static string TypeIncompatibleSpreadMessage(string fragName, object parentType, object fragType)
            => $"Fragment '{fragName}' cannot be spread here as objects of type '{parentType}' can never be of type '{fragType}'.";

        internal static string TypeIncompatibleAnonSpreadMessage(object parentType, object fragType)
            => $"Fragment cannot be spread here as objects of type '{parentType}' can never be of type '{fragType}'.";
    }
}
