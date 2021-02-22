using System;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.KnownDirectivesInAllowedLocations"/>
    [Serializable]
    public class DirectivesInAllowedLocationsError : ValidationError
    {
        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public DirectivesInAllowedLocationsError(ValidationContext context, Directive node, DirectiveLocation candidateLocation)
            : base(context.Document.OriginalQuery, "5.7.2", $"Directive '{node.Name}' may not be used on {candidateLocation}.", node)
        {
        }
    }
}
