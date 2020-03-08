using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQLParser;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Variables passed to field arguments conform to type
    /// </summary>
    public class VariablesInAllowedPosition : IValidationRule
    {
        public Func<string, string, string, string> BadVarPosMessage =>
            (varName, varType, expectedType) =>
                $"Variable \"${varName}\" of type \"{varType}\" used in position " +
                $"expecting type \"{expectedType}\".";

        public static readonly VariablesInAllowedPosition Instance = new VariablesInAllowedPosition();

        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            var varDefMap = new Dictionary<string, VariableDefinition>();

            return new EnterLeaveListener(_ =>
            {
                _.Match<VariableDefinition>(
                    varDefAst => varDefMap[varDefAst.Name] = varDefAst
                );

                _.Match<Operation>(
                    enter: op => varDefMap = new Dictionary<string, VariableDefinition>(),
                    leave: op =>
                    {
                        foreach (var usage in context.GetRecursiveVariables(op))
                        {
                            var varName = usage.Node.Name;
                            if (!varDefMap.TryGetValue(varName, out var varDef))
                            {
                                return;
                            }

                            if (varDef != null && usage.Type != null)
                            {
                                var varType = varDef.Type.GraphTypeFromType(context.Schema);
                                if (varType != null &&
                                    !effectiveType(varType, varDef).IsSubtypeOf(usage.Type, context.Schema))
                                {
                                    var error = new ValidationError(
                                        context.OriginalQuery,
                                        "5.7.6",
                                        BadVarPosMessage(varName, context.Print(varType), context.Print(usage.Type)));

                                    var source = new Source(context.OriginalQuery);
                                    var varDefPos = new Location(source, varDef.SourceLocation.Start);
                                    var usagePos = new Location(source, usage.Node.SourceLocation.Start);

                                    error.AddLocation(varDefPos.Line, varDefPos.Column);
                                    error.AddLocation(usagePos.Line, usagePos.Column);

                                    context.ReportError(error);
                                }
                            }
                        }
                    }
                );
            }).ToTask();
        }

        /// <summary>
        /// if a variable definition has a default value, it is effectively non-null.
        /// </summary>
        private GraphType effectiveType(IGraphType varType, VariableDefinition varDef)
        {
            if (varDef.DefaultValue == null || varType is NonNullGraphType)
            {
                return (GraphType)varType;
            }

            var type = varType.GetType();
            var genericType = typeof(NonNullGraphType<>).MakeGenericType(type);

            var nonNull = (NonNullGraphType)Activator.CreateInstance(genericType);
            nonNull.ResolvedType = varType;
            return nonNull;
        }
    }
}
