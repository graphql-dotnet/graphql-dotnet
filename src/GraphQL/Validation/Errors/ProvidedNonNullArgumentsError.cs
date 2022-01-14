using System;
using GraphQL.Types;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.ProvidedNonNullArguments"/>
    [Serializable]
    public class ProvidedNonNullArgumentsError : ValidationError
    {
        internal const string NUMBER = "5.4.2.1";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public ProvidedNonNullArgumentsError(ValidationContext context, GraphQLField node, QueryArgument arg)
            : base(context.OriginalQuery!, NUMBER, MissingFieldArgMessage(node.Name, arg.Name, arg.ResolvedType!.ToString()), node)
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public ProvidedNonNullArgumentsError(ValidationContext context, GraphQLDirective node, QueryArgument arg)
            : base(context.OriginalQuery!, NUMBER, MissingDirectiveArgMessage(node.Name, arg.Name, arg.ResolvedType!.ToString()), node)
        {
        }

        internal static string MissingFieldArgMessage(ROM fieldName, string argName, string type)
            => $"Argument '{argName}' of type '{type}' is required for field '{fieldName}' but not provided.";

        internal static string MissingDirectiveArgMessage(ROM directiveName, string argName, string type)
            => $"Argument '{argName}' of type '{type}' is required for directive '{directiveName}' but not provided.";
    }
}
