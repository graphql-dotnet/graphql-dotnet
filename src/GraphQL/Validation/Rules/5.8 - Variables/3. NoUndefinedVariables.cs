using GraphQL.Validation.Errors;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules;

/// <summary>
/// No undefined variables:
///
/// A GraphQL operation is only valid if all variables encountered, both directly
/// and via fragment spreads, are defined by that operation.
/// </summary>
public class NoUndefinedVariables : ValidationRuleBase
{
    /// <summary>
    /// Returns a static instance of this validation rule.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public static readonly NoUndefinedVariables Instance = new();
#pragma warning restore CS0618 // Type or member is obsolete

    /// <inheritdoc cref="NoUndefinedVariables"/>
    [Obsolete("Please use the Instance property to retrieve a static instance. This constructor will be removed in v9.")]
    public NoUndefinedVariables()
    {
    }

    /// <inheritdoc/>
    /// <exception cref="NoUndefinedVariablesError"/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_nodeVisitor);

    private static readonly INodeVisitor _nodeVisitor = new NodeVisitors(
        new MatchingNodeVisitor<GraphQLVariableDefinition>((varDef, context) =>
        {
            var varNameDef = context.TypeInfo.NoUndefinedVariables_VariableNameDefined ??= new();
            varNameDef.Add(varDef.Variable.Name);
        }),

        new MatchingNodeVisitor<GraphQLOperationDefinition>(
            enter: (op, context) => context.TypeInfo.NoUndefinedVariables_VariableNameDefined?.Clear(),
            leave: (op, context) =>
            {
                var varNameDef = context.TypeInfo.NoUndefinedVariables_VariableNameDefined;
                var usages = context.GetRecursiveVariables(op);
                if (usages != null)
                {
                    foreach (var usage in usages)
                    {
                        var varName = usage.Node.Name;
                        if (varNameDef == null || !varNameDef.Contains(varName))
                        {
                            context.ReportError(new NoUndefinedVariablesError(context, op, usage.Node));
                        }
                    }
                }
            })
    );
}
