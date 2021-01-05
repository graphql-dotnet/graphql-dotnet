using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Unique variable names:
    ///
    /// A GraphQL operation is only valid if all its variables are uniquely named.
    /// </summary>
    public class UniqueVariableNames : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly UniqueVariableNames Instance = new UniqueVariableNames();

        private static readonly Task<INodeVisitor> _task = new EnterLeaveListener(_ =>
            {
                _.Match<Operation>((__, context) => context.Set<UniqueVariableNames>(new Dictionary<string, VariableDefinition>()));

                _.Match<VariableDefinition>((variableDefinition, context) =>
                {
                    string variableName = variableDefinition.Name;
                    var knownVariables = context.Get<UniqueVariableNames, Dictionary<string, VariableDefinition>>();

                    if (knownVariables.ContainsKey(variableName))
                    {
                        context.ReportError(new UniqueVariableNamesError(context, knownVariables[variableName], variableDefinition));
                    }
                    else
                    {
                        knownVariables[variableName] = variableDefinition;
                    }
                });
            }).ToTask();

        /// <inheritdoc/>
        /// <exception cref="UniqueVariableNamesError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _task;
    }
}
