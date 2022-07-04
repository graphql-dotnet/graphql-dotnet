using GraphQLParser.AST;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.KnownDirectivesInAllowedLocations"/>
    [Serializable]
    public class KnownDirectivesError : ValidationError
    {
        internal const string NUMBER = "5.7.1";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public KnownDirectivesError(ValidationContext context, GraphQLDirective node)
            : base(context.Document.Source, NUMBER, $"Unknown directive '{node.Name}'.", node)
        {
        }
    }
}
