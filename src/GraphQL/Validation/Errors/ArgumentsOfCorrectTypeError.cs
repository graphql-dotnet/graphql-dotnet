using System;
using System.Collections.Generic;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.ArgumentsOfCorrectType"/>
    [Serializable]
    public class ArgumentsOfCorrectTypeError : ValidationError
    {
        internal const string NUMBER = "5.6.1";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public ArgumentsOfCorrectTypeError(ValidationContext context, Argument node, IEnumerable<string> verboseErrors)
            : base(context.Document.OriginalQuery, NUMBER, BadValueMessage(node.Name, context.Print(node.Value), verboseErrors), node)
        {
        }

        internal static string BadValueMessage(string argName, string value, IEnumerable<string> verboseErrors)
        {
            var message = verboseErrors != null ? $"\n{string.Join("\n", verboseErrors)}" : "";

            return $"Argument \"{argName}\" has invalid value {value}.{message}";
        }
    }
}
