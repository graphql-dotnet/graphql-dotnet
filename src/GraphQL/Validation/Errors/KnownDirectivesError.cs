using System;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.KnownDirectives"/>
    [Serializable]
    public class KnownDirectivesError : ValidationError
    {
        internal const string NUMBER = "5.7.1";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public KnownDirectivesError(ValidationContext context, Directive node)
            : base(context.Document.OriginalQuery, NUMBER, UnknownDirectiveMessage(node.Name), node)
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public KnownDirectivesError(ValidationContext context, Directive node, DirectiveLocation candidateLocation)
            : base(context.Document.OriginalQuery, NUMBER, MisplacedDirectiveMessage(node.Name, candidateLocation.ToString()), node)
        {
        }

        internal static string UnknownDirectiveMessage(string directiveName)
            => $"Unknown directive \"{directiveName}\".";

        internal static string MisplacedDirectiveMessage(string directiveName, string location)
            => $"Directive \"{directiveName}\" may not be used on {location}.";
    }
}
