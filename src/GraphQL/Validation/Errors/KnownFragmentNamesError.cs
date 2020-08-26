using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    public class KnownFragmentNamesError : ValidationError
    {
        public const string PARAGRAPH = "5.5.1.4";

        public KnownFragmentNamesError(ValidationContext context, FragmentSpread node, string fragmentName)
            : base(context.OriginalQuery, PARAGRAPH, UnknownFragmentMessage(fragmentName), node)
        {
        }

        internal static string UnknownFragmentMessage(string fragName)
            => $"Unknown fragment \"{fragName}\".";
    }
}
