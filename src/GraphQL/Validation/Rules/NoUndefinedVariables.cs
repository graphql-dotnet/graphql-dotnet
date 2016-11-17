using System;
using System.Collections.Generic;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// No undefined variables
    ///
    /// A GraphQL operation is only valid if all variables encountered, both directly
    /// and via fragment spreads, are defined by that operation.
    /// </summary>
    public class NoUndefinedVariables : IValidationRule
    {
        public Func<string, string, string> UndefinedVarMessage = (varName, opName) =>
            !string.IsNullOrWhiteSpace(opName)
                ? $"Variable \"${varName}\" is not defined by operation \"{opName}\"."
                : $"Variable \"${varName}\" is not defined.";

        public INodeVisitor Validate(ValidationContext context)
        {
            var variableNameDefined = new Dictionary<string, bool>();

            return new EnterLeaveListener(_ =>
            {
                _.Match<VariableDefinition>(varDef => variableNameDefined[varDef.Name] = true);

                _.Match<Operation>(
                    enter: op => variableNameDefined = new Dictionary<string, bool>(),
                    leave: op =>
                    {
                        var usages = context.GetRecursiveVariables(op);
                        usages.Apply(usage =>
                        {
                            var varName = usage.Node.Name;
                            bool found;
                            if (!variableNameDefined.TryGetValue(varName, out found))
                            {
                                var error = new ValidationError(
                                    context.OriginalQuery,
                                    "5.7.4",
                                    UndefinedVarMessage(varName, op.Name),
                                    usage.Node,
                                    op);
                                context.ReportError(error);
                            }
                        });
                    });
            });
        }
    }
}
