using System;
using System.Collections.Generic;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.DefaultValuesOfCorrectType"/>
    [Serializable]
    public class DefaultValuesOfCorrectTypeError : ValidationError
    {
        internal const string NUMBER = "5.6.1";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public DefaultValuesOfCorrectTypeError(ValidationContext context, VariableDefinition varDefAst, IGraphType inputType, IEnumerable<string> verboseErrors)
            : base(context.Document.OriginalQuery, NUMBER, BadValueForDefaultArgMessage(varDefAst.Name, inputType.ToString(), AstPrinter.Print(varDefAst.DefaultValue), verboseErrors), varDefAst.DefaultValue)
        {
        }

        internal static string BadValueForDefaultArgMessage(string varName, string type, string value, IEnumerable<string> verboseErrors)
        {
            var message = verboseErrors != null ? "\n" + string.Join("\n", verboseErrors) : "";
            return $"Variable \"{varName}\" of type \"{type}\" has invalid default value {value}.{message}";
        }
    }
}
