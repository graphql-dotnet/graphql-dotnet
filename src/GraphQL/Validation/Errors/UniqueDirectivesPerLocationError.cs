using System;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.UniqueDirectivesPerLocation"/>
    [Serializable]
    public class UniqueDirectivesPerLocationError : ValidationError
    {
        internal const string NUMBER = "5.7.3";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public UniqueDirectivesPerLocationError(ValidationContext context, Directive node, Directive altNode)
            : base(context.Document.OriginalQuery, NUMBER, DuplicateDirectiveMessage(node.Name), node, altNode)
        {
        }

        internal static string DuplicateDirectiveMessage(string directiveName)
            => $"The directive \"{directiveName}\" can only be used once at this location.";
    }
}
