using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    public class LoneAnonymousOperationError : ValidationError
    {
        public const string PARAGRAPH = "5.2.2.1";

        public LoneAnonymousOperationError(ValidationContext context, Operation node)
            : base(context.OriginalQuery, PARAGRAPH, AnonOperationNotAloneMessage(), node)
        {
        }

        internal static string AnonOperationNotAloneMessage()
            => "This anonymous operation must be the only defined operation.";
    }
}
