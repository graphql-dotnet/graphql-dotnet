using GraphQL.Validation.Errors;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules;

/// <summary>
/// No unused variables:
///
/// A GraphQL operation is only valid if all variables defined by that operation
/// are used in that operation or a fragment transitively included by that
/// operation.
/// </summary>
public class NoUnusedVariables : ValidationRuleBase
{
    /// <summary>
    /// Returns a static instance of this validation rule.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public static readonly NoUnusedVariables Instance = new();
#pragma warning restore CS0618 // Type or member is obsolete

    /// <inheritdoc cref="NoUnusedVariables"/>
    [Obsolete("Please use the Instance property to retrieve a static instance. This constructor will be removed in v9.")]
    public NoUnusedVariables()
    {
    }

    /// <inheritdoc/>
    /// <exception cref="NoUnusedVariablesError"/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_nodeVisitor);

    private static readonly INodeVisitor _nodeVisitor = new NodeVisitors(
        new MatchingNodeVisitor<GraphQLVariableDefinition>((def, context) =>
        {
            var varDefs = context.TypeInfo.NoUnusedVariables_VariableDefs ??= new();
            varDefs.Add(def);
        }),

        new MatchingNodeVisitor<GraphQLOperationDefinition>(
            enter: (op, context) => context.TypeInfo.NoUnusedVariables_VariableDefs?.Clear(),
            leave: (op, context) =>
            {
                var variableDefs = context.TypeInfo.NoUnusedVariables_VariableDefs;
                if (variableDefs == null || variableDefs.Count == 0)
                    return;

                var usages = context.GetRecursiveVariables(op);

                foreach (var variableDef in variableDefs)
                {
                    if (usages == null || !Contains(usages, variableDef))
                    {
                        context.ReportError(new NoUnusedVariablesError(context, variableDef, op));
                    }
                }

                static bool Contains(List<VariableUsage> usages, GraphQLVariableDefinition def)
                {
                    foreach (var usage in usages)
                    {
                        if (usage.Node.Name == def.Variable.Name)
                            return true;
                    }

                    return false;
                }
            })
    );
}
