using GraphQL.Validation.Errors;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules;

/// <summary>
/// Unique variable names:
///
/// A GraphQL operation is only valid if all its variables are uniquely named.
/// </summary>
public class UniqueVariableNames : ValidationRuleBase
{
    /// <summary>
    /// Returns a static instance of this validation rule.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public static readonly UniqueVariableNames Instance = new();
#pragma warning restore CS0618 // Type or member is obsolete

    /// <inheritdoc cref="UniqueVariableNames"/>
    [Obsolete("Please use the Instance property to retrieve a static instance. This constructor will be removed in v9.")]
    public UniqueVariableNames()
    {
    }

    /// <inheritdoc/>
    /// <exception cref="UniqueVariableNamesError"/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_nodeVisitor);

    private static readonly INodeVisitor _nodeVisitor = new NodeVisitors(
        new MatchingNodeVisitor<GraphQLOperationDefinition>((__, context) => context.TypeInfo.UniqueVariableNames_KnownVariables?.Clear()),
        new MatchingNodeVisitor<GraphQLVariableDefinition>((variableDefinition, context) =>
        {
            var knownVariables = context.TypeInfo.UniqueVariableNames_KnownVariables ??= new();

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
