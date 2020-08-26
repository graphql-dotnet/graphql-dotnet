using System;
using System.Collections.Generic;
using System.Text;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    public class ArgumentsOfCorrectTypeError : ValidationError
    {
        public ArgumentsOfCorrectTypeError(ValidationContext context, Argument node, IEnumerable<string> verboseErrors)
            : base(context.OriginalQuery, "5.6.1", BadValueMessage(node.Name, context.Print(node.Value), verboseErrors), node)
        {
        }

        private static string BadValueMessage(
            string argName,
            string value,
            IEnumerable<string> verboseErrors)
        {
            var message = verboseErrors != null ? $"\n{string.Join("\n", verboseErrors)}" : "";

            return $"Argument '{argName}' has invalid value \"{value}\".{message}";
        }
    }
}
