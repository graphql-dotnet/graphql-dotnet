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
        public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context) => new ValueTask<INodeVisitor?>(_nodeVisitor);

        private static readonly INodeVisitor _nodeVisitor = new NodeVisitors(
            new MatchingNodeVisitor<Operation>((__, context) => context.TypeInfo.UniqueVariableNames_KnownVariables?.Clear()),
            new MatchingNodeVisitor<VariableDefinition>((variableDefinition, context) =>
            {
                var knownVariables = context.TypeInfo.UniqueVariableNames_KnownVariables ??= new Dictionary<string, VariableDefinition>();

                var variableName = variableDefinition.Name;

                if (knownVariables.ContainsKey(variableName))
                {
                    context.ReportError(new UniqueVariableNamesError(context, knownVariables[variableName], variableDefinition));
                }
                else
                {
                    knownVariables[variableName] = variableDefinition;
                }
            })
        );
    }
}
