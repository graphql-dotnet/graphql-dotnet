using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

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
        public static readonly NoUndefinedVariables Instance = new NoUndefinedVariables();

        private static readonly Task<INodeVisitor> _task = new EnterLeaveListener(_ =>
            {
                _.Match<VariableDefinition>((varDef, context) => context.Get<NoUndefinedVariables, Dictionary<string, bool>>()[varDef.Name] = true);

                _.Match<Operation>(
                    enter: (op, context) => context.Set<NoUndefinedVariables>(new Dictionary<string, bool>()),
                    leave: (op, context) =>
                    {
                        var variableNameDefined = context.Get<NoUndefinedVariables, Dictionary<string, bool>>();

                        foreach (var usage in context.GetRecursiveVariables(op))
                        {
                            string varName = usage.Node.Name;
                            if (!variableNameDefined.TryGetValue(varName, out bool found))
                            {
                                context.ReportError(new NoUndefinedVariablesError(context, op, usage.Node));
                            }
                        }
                    });
            }).ToTask();

        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _task;
    }
}
