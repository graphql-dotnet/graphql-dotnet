using System;
using System.Collections.Generic;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    [Serializable]
    public class ArgumentsOfCorrectTypeError : ValidationError
    {
        internal const string NUMBER = "5.6.1";

        public ArgumentsOfCorrectTypeError(ValidationContext context, Argument node, IEnumerable<string> verboseErrors)
            : base(context.OriginalQuery, NUMBER, BadValueMessage(node.Name, context.Print(node.Value), verboseErrors), node)
        {
        }

        internal static string BadValueMessage(string argName, string value, IEnumerable<string> verboseErrors)
        {
            var message = verboseErrors != null ? $"\n{string.Join("\n", verboseErrors)}" : "";

            return $"Argument \"{argName}\" has invalid value {value}.{message}";
        }
    }
}
