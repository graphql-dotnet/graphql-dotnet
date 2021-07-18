using System;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.NoUnusedFragments"/>
    [Serializable]
    public class NoUnusedFragmentsError : ValidationError
    {
        internal const string NUMBER = "5.5.1.4";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public NoUnusedFragmentsError(ValidationContext context, FragmentDefinition node)
            : base(context.Document.OriginalQuery!, NUMBER, UnusedFragMessage(node.Name), node)
        {
        }

        internal static string UnusedFragMessage(string fragName)
            => $"Fragment '{fragName}' is never used.";
    }
}
