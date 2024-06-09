using GraphQL.Types;
using GraphQL.Validation.Errors;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules;

/// <summary>
/// Variables passed to field arguments conform to type.
/// </summary>
public class VariablesInAllowedPosition : ValidationRuleBase
{
    /// <summary>
    /// Returns a static instance of this validation rule.
    /// </summary>
    public static readonly VariablesInAllowedPosition Instance = new();

    /// <inheritdoc/>
    /// <exception cref="VariablesInAllowedPositionError"/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_nodeVisitor);

    private static readonly INodeVisitor _nodeVisitor = new NodeVisitors(
        new MatchingNodeVisitor<GraphQLVariableDefinition>(
            (varDefAst, context) =>
            {
                var varDefMap = context.TypeInfo.VariablesInAllowedPosition_VarDefMap ??= new();
                varDefMap[varDefAst.Variable.Name] = varDefAst;
            }
        ),

        new MatchingNodeVisitor<GraphQLOperationDefinition>(
            enter: (op, context) => context.TypeInfo.VariablesInAllowedPosition_VarDefMap?.Clear(),
            leave: (op, context) =>
            {
                var varDefMap = context.TypeInfo.VariablesInAllowedPosition_VarDefMap;
                if (varDefMap == null)
                    return;

                var usages = context.GetRecursiveVariables(op);
                if (usages != null)
                {
                    foreach (var usage in usages)
                    {
                        var varName = usage.Node.Name;
                        if (!varDefMap.TryGetValue(varName, out var varDef) || varDef == null)
                        {
                            return;
                        }

                        var varType = varDef.Type.GraphTypeFromType(context.Schema);

                        if (varType != null && usage.Type != null && !IsVariableUsageAllowed(varDef, varType, usage, context.Schema.Features.AllowScalarVariablesForListTypes))
                        {
                            context.ReportError(new VariablesInAllowedPositionError(context, varDef, varType, usage));
                        }
                    }
                }
            }
        )
    );

    private static bool IsVariableUsageAllowed(GraphQLVariableDefinition variableDefinition, IGraphType variableType, VariableUsage variableUsage, bool allowScalarsForLists)
    {
        // >> If locationType is a non-null type AND variableType is NOT a non-null type:
        if ((variableUsage.Type is NonNullGraphType || variableUsage.IsRequired) && variableType is not NonNullGraphType)
        {
            // >> Let hasNonNullVariableDefaultValue be true if a default value exists for variableDefinition and is not the value null
            var hasNonNullVariableDefaultValue = variableDefinition.DefaultValue != null;
            // >> Let hasLocationDefaultValue be true if a default value exists for the Argument or ObjectField where variableUsage is located.
            var hasLocationDefaultValue = variableUsage.HasDefault;
            // >> If hasNonNullVariableDefaultValue is NOT true AND hasLocationDefaultValue is NOT true, return false.
            if (!hasNonNullVariableDefaultValue && !hasLocationDefaultValue)
            {
                return false;
            }
            // >> Let nullableLocationType be the unwrapped nullable type of locationType.
            // NOTE: for OneOf Input Objects, the locationType will not have a nullability wrapper.
            var nullableLocationType = (variableUsage.Type as NonNullGraphType)?.ResolvedType! ?? variableUsage.Type;
            // >> Return AreTypesCompatible(variableType, nullableLocationType).
            return variableType.IsSubtypeOf(nullableLocationType, allowScalarsForLists);
        }
        // >> Return AreTypesCompatible(variableType, locationType).
        return variableType.IsSubtypeOf(variableUsage.Type, allowScalarsForLists);
    }
}
