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

    private static readonly INodeVisitor _nodeVisitor = new MatchingNodeVisitor<GraphQLDocument>(
        leave: (document, context) => ValidateFragments(context));

    private static void ValidateFragments(ValidationContext context)
    {
        var count = context.Document.FragmentsCount();
        var fragments = new Dictionary<ROM, (int definitionIndex, bool used)>(count);
        for (int i = 0; i < context.Document.Definitions.Count; ++i)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (context.Document.Definitions[i] is GraphQLFragmentDefinition fragment)
            {
                // Match FindFragmentDefinition's first-definition behavior for duplicate names.
#if NETSTANDARD2_0
                if (!fragments.ContainsKey(fragment.FragmentName.Name))
                    fragments.Add(fragment.FragmentName.Name, (i, false));
#else
                fragments.TryAdd(fragment.FragmentName.Name, (i, false));
#endif
            }
        }

        bool hasOperations = false;
        foreach (var definition in context.Document.Definitions)
        {
            if (definition is GraphQLOperationDefinition operation)
            {
                hasOperations = true;
                MarkUsedFragments(operation.SelectionSet, fragments, context);
            }
        }

        // Preserve the rule's behavior for documents without operations.
        if (!hasOperations)
            return;

        foreach (var definition in context.Document.Definitions)
        {
            if (definition is GraphQLFragmentDefinition fragment)
            {
                var entry = fragments[fragment.FragmentName.Name];
                if (!entry.used || !ReferenceEquals(context.Document.Definitions[entry.definitionIndex], fragment))
                    context.ReportError(new NoUnusedFragmentsError(context, fragment));
            }
        }
    }

    private static void MarkUsedFragments(
        GraphQLSelectionSet selectionSet,
        Dictionary<ROM, (int definitionIndex, bool used)> fragments,
        ValidationContext context)
    {
        // Share visited flags across operations, so each reachable fragment is traversed once.
        using var stack = new StackSlim<(List<ASTNode> selections, int selectionIndex)>();
        var selections = selectionSet.Selections;
        int selectionIndex = 0;

        while (true)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (selectionIndex == selections.Count)
            {
                if (!stack.TryPop(out var parent))
                    break;
                (selections, selectionIndex) = parent;
                continue;
            }

            var node = selections[selectionIndex++];
            GraphQLSelectionSet? childSelectionSet;
            if (node is GraphQLFragmentSpread spread)
            {
                var name = spread.FragmentName.Name;
                if (!fragments.TryGetValue(name, out var entry) || entry.used)
                    continue;

                // Mark before descending to handle cycles and repeated spreads.
                fragments[name] = (entry.definitionIndex, true);
                childSelectionSet = ((GraphQLFragmentDefinition)context.Document.Definitions[entry.definitionIndex]).SelectionSet;
            }
            else
            {
                childSelectionSet = node switch
                {
                    GraphQLField field => field.SelectionSet,
                    GraphQLInlineFragment inlineFragment => inlineFragment.SelectionSet,
                    _ => null,
                };
            }

            if (childSelectionSet == null)
                continue;
            if (selectionIndex < selections.Count)
                stack.Push((selections, selectionIndex));

            selections = childSelectionSet.Selections;
            selectionIndex = 0;
        }
    }
}
