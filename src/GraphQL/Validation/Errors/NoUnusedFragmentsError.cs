using System;
using GraphQLParser;
using GraphQLParser.AST;

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
        public NoUnusedFragmentsError(ValidationContext context, GraphQLFragmentDefinition node)
            : base(context.OriginalQuery!, NUMBER, UnusedFragMessage(node.Name), node)
        {
        }

        internal static string UnusedFragMessage(ROM fragName)
            => $"Fragment '{fragName}' is never used.";
    }
}
