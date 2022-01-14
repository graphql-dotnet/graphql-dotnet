using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Types;
using GraphQL.Validation.Errors;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Variables passed to field arguments conform to type.
    /// </summary>
    public class VariablesInAllowedPosition : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly VariablesInAllowedPosition Instance = new VariablesInAllowedPosition();

        /// <inheritdoc/>
        /// <exception cref="VariablesInAllowedPositionError"/>
        public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context) => new ValueTask<INodeVisitor?>(_nodeVisitor);

        private static readonly INodeVisitor _nodeVisitor = new NodeVisitors(
            new MatchingNodeVisitor<GraphQLVariableDefinition>(
                (varDefAst, context) =>
                {
                    var varDefMap = context.TypeInfo.VariablesInAllowedPosition_VarDefMap ??= new Dictionary<string, GraphQLVariableDefinition>();
                    varDefMap[(string)varDefAst.Variable.Name] = varDefAst; //TODO:!!!!alloc
                }
            ),

            new MatchingNodeVisitor<GraphQLOperationDefinition>(
                enter: (op, context) => context.TypeInfo.VariablesInAllowedPosition_VarDefMap?.Clear(),
                leave: (op, context) =>
                {
                    var varDefMap = context.TypeInfo.VariablesInAllowedPosition_VarDefMap;
                    if (varDefMap == null)
                        return;

                    foreach (var usage in context.GetRecursiveVariables(op))
                    {
                        var varName = usage.Node.Name;
                        if (!varDefMap.TryGetValue((string)varName, out var varDef)) //TODO:!!!!alloc
                        {
                            return;
                        }

                        if (varDef != null && usage.Type != null)
                        {
                            var varType = varDef.Type.GraphTypeFromType(context.Schema);
                            if (varType != null && !effectiveType(varType, varDef).IsSubtypeOf(usage.Type))
                            {
                                context.ReportError(new VariablesInAllowedPositionError(context, varDef, varType, usage));
                            }
                        }
                    }
                }
            )
        );

        /// <summary>
        /// if a variable definition has a default value, it is effectively non-null.
        /// </summary>
        private static GraphType effectiveType(IGraphType varType, GraphQLVariableDefinition varDef)
        {
            if (varDef.DefaultValue == null || varType is NonNullGraphType)
            {
                return (GraphType)varType;
            }

            var type = varType.GetType();
            var genericType = typeof(NonNullGraphType<>).MakeGenericType(type);

            var nonNull = (NonNullGraphType)Activator.CreateInstance(genericType)!;
            nonNull.ResolvedType = varType;
            return nonNull;
        }
    }
}
