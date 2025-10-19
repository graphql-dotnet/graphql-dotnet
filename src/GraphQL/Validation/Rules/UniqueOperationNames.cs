using GraphQL.Validation.Errors;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules;

/// <summary>
/// Unique operation names:
///
/// A GraphQL document is only valid if all defined operations have unique names.
/// </summary>
public class UniqueOperationNames : ValidationRuleBase
{
    /// <summary>
    /// Returns a static instance of this validation rule.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public static readonly UniqueOperationNames Instance = new();
#pragma warning restore CS0618 // Type or member is obsolete

    /// <inheritdoc cref="UniqueOperationNames"/>
    [Obsolete("Please use the Instance property to retrieve a static instance. This constructor will be removed in v9.")]
    public UniqueOperationNames()
    {
    }

    /// <inheritdoc/>
    /// <exception cref="UniqueOperationNamesError"/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
        => new(context.Document.OperationsCount() < 2 ? null : _nodeVisitor);

    private static readonly INodeVisitor _nodeVisitor = new MatchingNodeVisitor<GraphQLOperationDefinition>((op, context) =>
    {
        if (op.Name is null)
        {
            return;
        }

        var frequency = context.TypeInfo.UniqueOperationNames_Frequency ??= [];

        if (!frequency.Add(op.Name))
        {
            context.ReportError(new UniqueOperationNamesError(context, op));
        }
    });
}
