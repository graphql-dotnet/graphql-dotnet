using System;
using GraphQLParser.AST;

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
        public UniqueDirectivesPerLocationError(ValidationContext context, GraphQLDirective node)
            : base(context.OriginalQuery!, NUMBER, $"The directive '{node.Name}' can only be used once at this location.", node)
        {
        }
    }
}
