using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Unique variable names
    ///
    /// A GraphQL operation is only valid if all its variables are uniquely named.
    /// </summary>
    public class UniqueVariableNames : IValidationRule
    {
        public static readonly UniqueVariableNames Instance = new UniqueVariableNames();

        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            Dictionary<string, VariableDefinition> knownVariables = null;

            return new EnterLeaveListener(_ =>
            {
                _.Match<Operation>(__ => knownVariables = new Dictionary<string, VariableDefinition>());

                _.Match<VariableDefinition>(variableDefinition =>
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
