using GraphQL.Validation.Errors;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules;

/// <summary>
/// No unused fragments:
///
/// A GraphQL document is only valid if all fragment definitions are spread
/// within operations, or spread within other fragment spreads within operations.
/// </summary>
public class NoUnusedFragments : ValidationRuleBase
{
    /// <summary>
    /// Returns a static instance of this validation rule.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public static readonly NoUnusedFragments Instance = new();
#pragma warning restore CS0618 // Type or member is obsolete

    /// <inheritdoc cref="NoUnusedFragments"/>
    [Obsolete("Please use the Instance property to retrieve a static instance. This constructor will be removed in v9.")]
    public NoUnusedFragments()
    {
    }

    /// <inheritdoc/>
    /// <exception cref="NoUnusedFragmentsError"/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
        => new(context.Document.FragmentsCount() > 0 ? _nodeVisitor : null);

    private static readonly INodeVisitor _nodeVisitor = new NodeVisitors(
        new MatchingNodeVisitor<GraphQLOperationDefinition>((node, context) => (context.TypeInfo.NoUnusedFragments_OperationDefs ??= new(1)).Add(node)),
        new MatchingNodeVisitor<GraphQLFragmentDefinition>((node, context) => (context.TypeInfo.NoUnusedFragments_FragmentDefs ??= []).Add(node)),
        new MatchingNodeVisitor<GraphQLDocument>(leave: (document, context) =>
        {
            var fragmentDefs = context.TypeInfo.NoUnusedFragments_FragmentDefs;
            if (fragmentDefs == null)
                return;

            var operationDefs = context.TypeInfo.NoUnusedFragments_OperationDefs;
            if (operationDefs == null)
                return;

            var fragmentsUsed = context.GetRecursivelyReferencedFragments(operationDefs);

            foreach (var fragmentDef in fragmentDefs)
            {
                if (fragmentsUsed == null || !fragmentsUsed.Contains(fragmentDef))
                {
                    context.ReportError(new NoUnusedFragmentsError(context, fragmentDef));
                }
            }
        })
    );
}
