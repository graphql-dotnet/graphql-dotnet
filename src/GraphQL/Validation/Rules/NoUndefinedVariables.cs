using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// No undefined variables:
    ///
    /// A GraphQL operation is only valid if all variables encountered, both directly
    /// and via fragment spreads, are defined by that operation.
    /// </summary>
    public class NoUndefinedVariables : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly NoUndefinedVariables Instance = new NoUndefinedVariables();

        /// <inheritdoc/>
        /// <exception cref="NoUndefinedVariablesError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            var variableNameDefined = new Dictionary<string, bool>();

            return new EnterLeaveListener(_ =>
            {
                _.Match<VariableDefinition>((varDef, context) => variableNameDefined[varDef.Name] = true);

                _.Match<Operation>(
                    enter: (op, context) => variableNameDefined = new Dictionary<string, bool>(),
                    leave: (op, context) =>
                    {
                        foreach (var usage in context.GetRecursiveVariables(op))
                        {
                            var varName = usage.Node.Name;
                            if (!variableNameDefined.TryGetValue(varName, out bool found))
                            {
                                context.ReportError(new NoUndefinedVariablesError(context, op, usage.Node));
                            }
                        }
                    });
            }).ToTask();
        }
    }
}
