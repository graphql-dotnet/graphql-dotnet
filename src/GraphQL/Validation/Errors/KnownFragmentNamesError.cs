using System;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    [Serializable]
    public class KnownFragmentNamesError : ValidationError
    {
        internal const string NUMBER = "5.5.1.1";

        public KnownFragmentNamesError(ValidationContext context, FragmentSpread node, string fragmentName)
            : base(context.OriginalQuery, NUMBER, UnknownFragmentMessage(fragmentName), node)
        {
        }

        internal static string UnknownFragmentMessage(string fragName)
            => $"Unknown fragment \"{fragName}\".";
    }
}
