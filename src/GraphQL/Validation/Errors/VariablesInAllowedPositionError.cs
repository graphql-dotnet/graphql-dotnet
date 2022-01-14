using System;
using GraphQL.Types;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.VariablesInAllowedPosition"/>
    [Serializable]
    public class VariablesInAllowedPositionError : ValidationError
    {
        internal const string NUMBER = "5.8.5";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public VariablesInAllowedPositionError(ValidationContext context, GraphQLVariableDefinition varDef, IGraphType varType, VariableUsage usage)
            : base(context.OriginalQuery!, NUMBER, BadVarPosMessage(usage.Node.Name, varType.ToString(), usage.Type.ToString()))
        {
            var varDefPos = new Location(context.OriginalQuery!, varDef.Location.Start);
            var usagePos = new Location(context.OriginalQuery!, usage.Node.Location.Start);

            AddLocation(varDefPos.Line, varDefPos.Column);
            AddLocation(usagePos.Line, usagePos.Column);
        }

        internal static string BadVarPosMessage(ROM varName, string varType, string expectedType)
            => $"Variable '${varName}' of type '{varType}' used in position expecting type '{expectedType}'.";
    }
}
