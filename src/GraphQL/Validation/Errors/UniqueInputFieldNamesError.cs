using System;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.UniqueInputFieldNames"/>
    [Serializable]
    public class UniqueInputFieldNamesError : ValidationError
    {
        internal const string NUMBER = "5.6.3";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public UniqueInputFieldNamesError(ValidationContext context, IValue node, ObjectField altNode)
            : base(context.Document.OriginalQuery!, NUMBER, DuplicateInputField(altNode.Name), node, altNode.Value)
        {
        }

        internal static string DuplicateInputField(string fieldName)
            => $"There can be only one input field named {fieldName}.";
    }
}
