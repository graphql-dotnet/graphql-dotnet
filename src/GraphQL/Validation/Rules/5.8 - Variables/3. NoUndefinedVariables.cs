#nullable enable

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
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _nodeVisitor;

        private static readonly Task<INodeVisitor> _nodeVisitor = new NodeVisitors(
            new MatchingNodeVisitor<VariableDefinition>((varDef, context) =>
            {
                var varNameDef = context.TypeInfo.NoUndefinedVariables_VariableNameDefined ??= new HashSet<string>();
                varNameDef.Add(varDef.Name);
            }),

            new MatchingNodeVisitor<Operation>(
                enter: (op, context) => context.TypeInfo.NoUndefinedVariables_VariableNameDefined?.Clear(),
                leave: (op, context) =>
                {
                    var varNameDef = context.TypeInfo.NoUndefinedVariables_VariableNameDefined;
                    foreach (var usage in context.GetRecursiveVariables(op))
                    {
                        var varName = usage.Node.Name;
                        if (varNameDef == null || !varNameDef.Contains(varName))
                        {
                            context.ReportError(new NoUndefinedVariablesError(context, op, usage.Node));
                        }
                    }
                })
        ).ToTask();
    }
}
