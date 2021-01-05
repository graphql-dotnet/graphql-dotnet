using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Validation.Errors;

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

        private static readonly Task<INodeVisitor> _task = new EnterLeaveListener(_ =>
            {
                _.Match<VariableDefinition>((varDefAst, context) => context.Get<VariablesInAllowedPosition, Dictionary<string, VariableDefinition>>()[varDefAst.Name] = varDefAst);

                _.Match<Operation>(
                    enter: (op, context) => context.Set<VariablesInAllowedPosition>(new Dictionary<string, VariableDefinition>()),
                    leave: (op, context) =>
                    {
                        var varDefMap = context.Get<VariablesInAllowedPosition, Dictionary<string, VariableDefinition>>();
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
                                    context.ReportError(new VariablesInAllowedPositionError(context, varDef, varType, usage));
                                }
                            }
                        }
                    }
                );
            }).ToTask();

        /// <inheritdoc/>
        /// <exception cref="VariablesInAllowedPositionError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _task;

        /// <summary>
        /// if a variable definition has a default value, it is effectively non-null.
        /// </summary>
        private static GraphType effectiveType(IGraphType varType, VariableDefinition varDef)
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
