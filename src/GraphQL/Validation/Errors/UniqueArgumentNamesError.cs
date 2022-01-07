using System;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.UniqueArgumentNames"/>
    [Serializable]
    public class UniqueArgumentNamesError : ValidationError
    {
        internal const string NUMBER = "5.4.2";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public UniqueArgumentNamesError(ValidationContext context, Argument node, Argument otherNode)
            : base(context.Document.OriginalQuery!, NUMBER, DuplicateArgMessage(node.Name), node, otherNode)
        {
        }

        internal static string DuplicateArgMessage(string argName)
            => $"There can be only one argument named '{argName}'.";
    }
}
