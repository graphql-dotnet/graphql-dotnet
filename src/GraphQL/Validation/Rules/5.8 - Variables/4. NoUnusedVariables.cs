#nullable enable

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

        /// <inheritdoc/>
        /// <exception cref="NoUnusedVariablesError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _nodeVisitor;

        private static readonly Task<INodeVisitor> _nodeVisitor = new NodeVisitors(
            new MatchingNodeVisitor<VariableDefinition>((def, context) =>
            {
                var varDefs = context.TypeInfo.NoUnusedVariables_VariableDefs ??= new List<VariableDefinition>();
                varDefs.Add(def);
            }),

            new MatchingNodeVisitor<Operation>(
                enter: (op, context) => context.TypeInfo.NoUnusedVariables_VariableDefs?.Clear(),
                leave: (op, context) =>
                {
                    var variableDefs = context.TypeInfo.NoUnusedVariables_VariableDefs;
                    if (variableDefs == null || variableDefs.Count == 0)
                        return;

                    var usages = context.GetRecursiveVariables(op)
                        .Select(usage => usage.Node.Name)
                        .ToList();

                    foreach (var variableDef in variableDefs)
                    {
                        var variableName = variableDef.Name;
                        if (!usages.Contains(variableName))
                        {
                            context.ReportError(new NoUnusedVariablesError(context, variableDef, op));
                        }
                    }
                })
        ).ToTask();
    }
}
