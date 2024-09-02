using GraphQL.Types;
using GraphQL.Validation.Errors;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules;

/// <summary>
/// Variables are input types:
///
/// A GraphQL operation is only valid if all the variables it defines are of
/// input types (scalar, enum, or input object).
/// </summary>
public class VariablesAreInputTypes : ValidationRuleBase
{
    /// <summary>
    /// Returns a static instance of this validation rule.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public static readonly VariablesAreInputTypes Instance = new();
#pragma warning restore CS0618 // Type or member is obsolete

    /// <inheritdoc cref="VariablesAreInputTypes"/>
    [Obsolete("Please use the Instance property to retrieve a static instance. This constructor will be removed in v9.")]
    public VariablesAreInputTypes()
    {
    }

    /// <inheritdoc/>
    /// <exception cref="VariablesAreInputTypesError"/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_nodeVisitor);

    private static readonly INodeVisitor _nodeVisitor = new MatchingNodeVisitor<GraphQLVariableDefinition>((varDef, context) =>
    {
        var type = varDef.Type.NamedGraphTypeFromType(context.Schema);

        if (type == null || !type.IsInputType())
        {
            context.ReportError(new VariablesAreInputTypesError(context, varDef, varDef.Type.GraphTypeFromType(context.Schema)!));
        }
    });
}
