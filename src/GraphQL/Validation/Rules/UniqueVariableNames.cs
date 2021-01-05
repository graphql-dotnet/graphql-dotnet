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

        /// <inheritdoc/>
        /// <exception cref="UniqueVariableNamesError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            Dictionary<string, VariableDefinition> knownVariables = null;

            return new EnterLeaveListener(_ =>
            {
                _.Match<Operation>((__, context) => knownVariables = new Dictionary<string, VariableDefinition>());

                _.Match<VariableDefinition>((variableDefinition, context) =>
                {
                    var variableName = variableDefinition.Name;

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
        }
    }
}
