using System;
using GraphQL.Language.AST;
using GraphQL.Utilities;

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
        public FragmentsOnCompositeTypesError(ValidationContext context, InlineFragment node)
            : base(context.Document.OriginalQuery, NUMBER, InlineFragmentOnNonCompositeErrorMessage(node.Type.ToString(context.Document)), node.Type)
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public FragmentsOnCompositeTypesError(ValidationContext context, FragmentDefinition node)
            : base(context.Document.OriginalQuery, NUMBER, FragmentOnNonCompositeErrorMessage(node.Name, node.Type.ToString(context.Document)), node.Type)
        {
        }

        internal static string InlineFragmentOnNonCompositeErrorMessage(string type)
            => $"Fragment cannot condition on non composite type '{type}'.";

        internal static string FragmentOnNonCompositeErrorMessage(string fragName, string type)
            => $"Fragment '{fragName}' cannot condition on non composite type '{type}'.";
    }
}
