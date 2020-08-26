using System.Collections.Generic;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation.Errors
{
    public class DefaultValuesOfCorrectTypeError : ValidationError
    {
        public const string PARAGRAPH = "5.6.1";

        public DefaultValuesOfCorrectTypeError(ValidationContext context, IValue node, string message)
            : base(context.OriginalQuery, PARAGRAPH, message, node)
        {
        }

        public DefaultValuesOfCorrectTypeError(ValidationContext context, VariableDefinition varDefAst, NonNullGraphType nonNullType)
            : base(context.OriginalQuery, PARAGRAPH, BadValueForNonNullArgMessage(varDefAst.Name, context.Print(nonNullType), context.Print(nonNullType.ResolvedType)), varDefAst.DefaultValue)
        {
        }

        public DefaultValuesOfCorrectTypeError(ValidationContext context, VariableDefinition varDefAst, IGraphType inputType, IEnumerable<string> verboseErrors)
            : base(context.OriginalQuery, PARAGRAPH, BadValueForDefaultArgMessage(varDefAst.Name, context.Print(inputType), context.Print(varDefAst.DefaultValue), verboseErrors), varDefAst.DefaultValue)
        {
        }

        internal static string BadValueForNonNullArgMessage(string varName, string type, string guessType)
            => $"Variable \"{varName}\" of type \"{type}\" is required and will not use default value. " +
               $"Perhaps you mean to use type \"{guessType}\"?";

        internal static string BadValueForDefaultArgMessage(string varName, string type, string value, IEnumerable<string> verboseErrors)
        {
            var message = verboseErrors != null ? "\n" + string.Join("\n", verboseErrors) : "";
            return $"Variable \"{varName}\" of type \"{type}\" has invalid default value {value}.{message}";
        }
    }
}
