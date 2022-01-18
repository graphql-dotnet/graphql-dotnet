using System;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.FragmentsOnCompositeTypes"/>
    [Serializable]
    public class FragmentsOnCompositeTypesError : ValidationError
    {
        internal const string NUMBER = "5.5.1.3";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public FragmentsOnCompositeTypesError(ValidationContext context, GraphQLInlineFragment node)
            : base(context.OriginalQuery!, NUMBER, InlineFragmentOnNonCompositeErrorMessage(node.TypeCondition!.Type.StringFrom(context.OriginalQuery)), node.TypeCondition!.Type)
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public FragmentsOnCompositeTypesError(ValidationContext context, GraphQLFragmentDefinition node)
            : base(context.OriginalQuery!, NUMBER, FragmentOnNonCompositeErrorMessage(node.FragmentName.Name, node.TypeCondition.Type.StringFrom(context.OriginalQuery)), node.TypeCondition.Type)
        {
        }

        internal static string InlineFragmentOnNonCompositeErrorMessage(ROM type)
            => $"Fragment cannot condition on non composite type '{type}'.";

        internal static string FragmentOnNonCompositeErrorMessage(ROM fragName, string type)
            => $"Fragment '{fragName}' cannot condition on non composite type '{type}'.";
    }
}
