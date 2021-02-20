using System;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.LoneAnonymousOperation"/>
    [Serializable]
    public class LoneAnonymousOperationError : ValidationError
    {
        internal const string NUMBER = "5.2.2.1";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public LoneAnonymousOperationError(ValidationContext context, Operation node)
            : base(context.Document.OriginalQuery, NUMBER, AnonOperationNotAloneMessage(), node)
        {
        }

        internal static string AnonOperationNotAloneMessage()
            => "This anonymous operation must be the only defined operation.";
    }
}
