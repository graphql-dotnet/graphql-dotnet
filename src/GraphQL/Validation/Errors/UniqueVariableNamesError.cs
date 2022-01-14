using System;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.UniqueVariableNames"/>
    [Serializable]
    public class UniqueVariableNamesError : ValidationError
    {
        internal const string NUMBER = "5.8.1";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public UniqueVariableNamesError(ValidationContext context, GraphQLVariableDefinition node, GraphQLVariableDefinition altNode)
            : base(context.OriginalQuery!, NUMBER, DuplicateVariableMessage(node.Variable.Name), node, altNode)
        {
        }

        internal static string DuplicateVariableMessage(ROM variableName)
            => $"There can be only one variable named '{variableName}'";
    }
}
