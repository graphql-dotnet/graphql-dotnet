using System;
using GraphQL.Types;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.VariablesAreInputTypes"/>
    [Serializable]
    public class VariablesAreInputTypesError : ValidationError
    {
        internal const string NUMBER = "5.8.2";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public VariablesAreInputTypesError(ValidationContext context, GraphQLVariableDefinition node, IGraphType type)
            : base(context.OriginalQuery!, NUMBER, UndefinedVarMessage(node.Variable.Name, type?.ToString() ?? node.Type.Name()), node)
        {
        }

        internal static string UndefinedVarMessage(ROM variableName, ROM typeName)
            => $"Variable '{variableName}' cannot be non-input type '{typeName}'.";
    }
}
