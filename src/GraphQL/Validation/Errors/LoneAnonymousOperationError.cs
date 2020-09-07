using System;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    [Serializable]
    public class LoneAnonymousOperationError : ValidationError
    {
        internal const string NUMBER = "5.2.2.1";

        public LoneAnonymousOperationError(ValidationContext context, Operation node)
            : base(context.OriginalQuery, NUMBER, AnonOperationNotAloneMessage(), node)
        {
        }

        internal static string AnonOperationNotAloneMessage()
            => "This anonymous operation must be the only defined operation.";
    }
}
