using System;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQLParser;

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
        public VariablesInAllowedPositionError(ValidationContext context, VariableDefinition varDef, IGraphType varType, VariableUsage usage)
            : base(context.Document.OriginalQuery, NUMBER, BadVarPosMessage(usage.Node.Name, context.Print(varType), context.Print(usage.Type)))
        {
            var varDefPos = new Location(context.Document.OriginalQuery, varDef.SourceLocation.Start);
            var usagePos = new Location(context.Document.OriginalQuery, usage.Node.SourceLocation.Start);

            AddLocation(varDefPos.Line, varDefPos.Column);
            AddLocation(usagePos.Line, usagePos.Column);
        }

        internal static string BadVarPosMessage(string varName, string varType, string expectedType)
            => $"Variable \"${varName}\" of type \"{varType}\" used in position expecting type \"{expectedType}\".";
    }
}
