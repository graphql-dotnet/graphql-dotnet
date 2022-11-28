using GraphQLParser.AST;

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
        public ArgumentsOfCorrectTypeError(ValidationContext context, GraphQLArgument node, string verboseErrors)
            : base(context.Document.Source, NUMBER, BadValueMessage(node.Name.StringValue, verboseErrors), node)
        {
        }

        internal static string BadValueMessage(string argName, string verboseErrors)
        {
            return string.IsNullOrEmpty(verboseErrors)
                ? $"Argument '{argName}' has invalid value."
                : $"Argument '{argName}' has invalid value. {verboseErrors}";
        }
    }
}
