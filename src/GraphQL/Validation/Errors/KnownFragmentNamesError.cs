using System;
using GraphQLParser;
using GraphQLParser.AST;

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
        public KnownFragmentNamesError(ValidationContext context, GraphQLFragmentSpread node, ROM fragmentName)
            : base(context.Document.Source, NUMBER, UnknownFragmentMessage(fragmentName), node)
        {
        }

        internal static string UnknownFragmentMessage(ROM fragName)
            => $"Unknown fragment '{fragName}'.";
    }
}
