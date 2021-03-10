using System;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.KnownFragmentNames"/>
    [Serializable]
    public class KnownFragmentNamesError : ValidationError
    {
        internal const string NUMBER = "5.5.2.1";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public KnownFragmentNamesError(ValidationContext context, FragmentSpread node, string fragmentName)
            : base(context.Document.OriginalQuery, NUMBER, UnknownFragmentMessage(fragmentName), node)
        {
        }

        internal static string UnknownFragmentMessage(string fragName)
            => $"Unknown fragment '{fragName}'.";
    }
}
