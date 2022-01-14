using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Validation.Errors;
using GraphQLParser;
using GraphQLParser.AST;

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
            new MatchingNodeVisitor<GraphQLOperationDefinition>((__, context) => context.TypeInfo.UniqueVariableNames_KnownVariables?.Clear()),
            new MatchingNodeVisitor<GraphQLVariableDefinition>((variableDefinition, context) =>
            {
                var knownVariables = context.TypeInfo.UniqueVariableNames_KnownVariables ??= new Dictionary<ROM, GraphQLVariableDefinition>();

                var variableName = variableDefinition.Variable.Name;

                if (knownVariables.TryGetValue(variableName, out var variable))
                {
                    context.ReportError(new UniqueVariableNamesError(context, variable, variableDefinition));
                }
                else
                {
                    knownVariables[variableName] = variableDefinition;
                }
            })
        );
    }
}
