using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// No unused variables:
    ///
    /// A GraphQL operation is only valid if all variables defined by that operation
    /// are used in that operation or a fragment transitively included by that
    /// operation.
    /// </summary>
    public class NoUnusedVariables : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly NoUnusedVariables Instance = new NoUnusedVariables();

        private static readonly Task<INodeVisitor> _task = new EnterLeaveListener(_ =>
            {
                _.Match<VariableDefinition>((def, context) => context.Get<NoUnusedVariables, List<VariableDefinition>>().Add(def));

                _.Match<Operation>(
                enter: (op, context) => context.Set<NoUnusedVariables>(new List<VariableDefinition>()),
                leave: (op, context) =>
                {
                    var variableDefs = context.Get<NoUnusedVariables, List<VariableDefinition>>();

                    var usages = context.GetRecursiveVariables(op)
                        .Select(usage => usage.Node.Name)
                        .ToList();

                    foreach (var variableDef in variableDefs)
                    {
                        string variableName = variableDef.Name;
                        if (!usages.Contains(variableName))
                        {
                            context.ReportError(new NoUnusedVariablesError(context, variableDef, op));
                        }
                    }
                });
            }).ToTask();

        /// <inheritdoc/>
        /// <exception cref="NoUnusedVariablesError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _task;
    }
}
