using System;
using System.Collections.Generic;
using GraphQL.Language;
using GraphQL.Types;

namespace GraphQL.Validation.Rules
{
    public class VariablesInAllowedPosition : IValidationRule
    {
        public Func<string, string, string, string> BadVarPosMessage =>
            (varName, varType, expectedType) =>
                $"Variable \"${varName}\" of type \"{varType}\" used in position " +
                $"expecting type \"{expectedType}\".";

        public INodeVisitor Validate(ValidationContext context)
        {
            var varDefMap = new Dictionary<string, VariableDefinition>();

            return new EnterLeaveFuncListener(_ =>
            {
                _.Add<VariableDefinition>(
                    n => n is VariableDefinition,
                    enter: varDefAst => varDefMap[varDefAst.Name] = varDefAst
                );

                _.Add<Operation>(
                    n => n is Operation,
                    enter: op => varDefMap = new Dictionary<string, VariableDefinition>(),
                    leave: op =>
                    {
                        var usages = context.GetRecursiveVariables(op);
                        usages.Apply(usage =>
                        {
                            var varName = usage.Node.Name;
                            VariableDefinition varDef;
                            if (!varDefMap.TryGetValue(varName, out varDef))
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
                                        "5.7.6",
                                        BadVarPosMessage(varName, context.Print(varType), context.Print(usage.Type)));
                                    error.AddLocation(varDef.SourceLocation.Line, varDef.SourceLocation.Column);
                                    error.AddLocation(usage.Node.SourceLocation.Line, usage.Node.SourceLocation.Column);
                                    context.ReportError(error);
                                }
                            }
                        });
                    }
                );
            });
        }

        // if a variable defintion has a default value, it is effectively non-null.
        private GraphType effectiveType(GraphType varType, VariableDefinition varDef)
        {
            if (varDef.DefaultValue == null || varType is NonNullGraphType)
            {
                return varType;
            }

            var type = varType.GetType();
            var genericType = typeof(NonNullGraphType<>).MakeGenericType(type);

            return (GraphType)Activator.CreateInstance(genericType);
        }
    }
}
