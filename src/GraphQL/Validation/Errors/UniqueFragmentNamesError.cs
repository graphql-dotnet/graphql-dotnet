using System;
using GraphQL.Language.AST;

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
        public UniqueFragmentNamesError(ValidationContext context, FragmentDefinition node, FragmentDefinition altNode)
            : base(context.OriginalQuery, NUMBER, DuplicateFragmentNameMessage((string)node.Name), node, altNode)
        {
        }

        internal static string DuplicateFragmentNameMessage(string fragName)
            => $"There can only be one fragment named \"{fragName}\"";
    }
}
