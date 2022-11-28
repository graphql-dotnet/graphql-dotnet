using GraphQL.Types;
using GraphQLParser.AST;

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
        public DefaultValuesOfCorrectTypeError(ValidationContext context, GraphQLVariableDefinition varDefAst, IGraphType inputType, string verboseErrors)
            : base(context.Document.Source, NUMBER, BadValueForDefaultArgMessage(varDefAst.Variable.Name.StringValue, inputType.ToString()!, varDefAst.DefaultValue!.Print(), verboseErrors), varDefAst.DefaultValue!)
        {
        }

        internal static string BadValueForDefaultArgMessage(string varName, string type, string value, string verboseErrors)
        {
            return string.IsNullOrEmpty(verboseErrors)
                ? $"Variable '{varName}' of type '{type}' has invalid default value '{value}'."
                : $"Variable '{varName}' of type '{type}' has invalid default value '{value}'. {verboseErrors}";
        }
    }
}
