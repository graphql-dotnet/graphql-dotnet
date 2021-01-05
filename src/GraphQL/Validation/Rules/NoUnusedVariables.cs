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
        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            var variableDefs = new List<VariableDefinition>();

            return new NodeVisitors(
                new MatchingNodeVisitor<VariableDefinition>((def, context) => variableDefs.Add(def)),

                new MatchingNodeVisitor<Operation>(
                    enter: (op, context) => variableDefs = new List<VariableDefinition>(),
                    leave: (op, context) =>
                    {
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
}
