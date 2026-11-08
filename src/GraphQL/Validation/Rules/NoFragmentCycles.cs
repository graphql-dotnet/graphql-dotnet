using GraphQL.Validation.Errors;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules;

/// <summary>
/// No fragment cycles:
///
/// A GraphQL document is only valid if it does not contain fragment cycles.
/// </summary>
public class NoFragmentCycles : ValidationRuleBase
{
    /// <summary>
    /// Returns a static instance of this validation rule.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public static readonly NoFragmentCycles Instance = new();
#pragma warning restore CS0618 // Type or member is obsolete

    /// <inheritdoc cref="NoFragmentCycles"/>
    [Obsolete("Please use the Instance property to retrieve a static instance. This constructor will be removed in v9.")]
    public NoFragmentCycles()
    {
    }

    /// <inheritdoc/>
    /// <exception cref="NoFragmentCyclesError"/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
    {
        if (context.Document.FragmentsCount() == 0)
            return default;

        context.CancellationToken.ThrowIfCancellationRequested();

        // Cycle detection needs no TypeInfo, so keep its state local and finish before the AST walk.
        var unvisitedFragments = GetFragmentDefinitions(context.Document);
        var spreadPath = new Stack<GraphQLFragmentSpread>();
        var spreadPathIndexByName = new Dictionary<ROM, int>();
        foreach (var definition in context.Document.Definitions)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (definition is GraphQLFragmentDefinition fragment && unvisitedFragments[fragment.FragmentName.Name] != null)
                DetectCycles(fragment, spreadPath, unvisitedFragments, spreadPathIndexByName, context);
        }

        return default;
    }

    private static Dictionary<ROM, GraphQLFragmentDefinition?> GetFragmentDefinitions(GraphQLDocument document)
    {
        var fragments = new Dictionary<ROM, GraphQLFragmentDefinition?>(document.FragmentsCount());
        foreach (var definition in document.Definitions)
        {
            // Match FindFragmentDefinition's first-definition behavior for duplicate names.
            if (definition is GraphQLFragmentDefinition fragment && !fragments.ContainsKey(fragment.FragmentName.Name))
                fragments.Add(fragment.FragmentName.Name, fragment);
        }
        return fragments;
    }

    /// <summary>
    /// Traverses fragment references iteratively so deeply nested chains cannot overflow the call stack.
    /// </summary>
    private static void DetectCycles(
        GraphQLFragmentDefinition fragment,
        Stack<GraphQLFragmentSpread> spreadPath,
        Dictionary<ROM, GraphQLFragmentDefinition?> unvisitedFragments,
        Dictionary<ROM, int> spreadPathIndexByName,
        ValidationContext context)
    {
        var fragmentName = fragment.FragmentName.Name;
        // Null entries mark visited definitions without a separate visited set or repeated document scans.
        unvisitedFragments[fragmentName] = null;

        var spreadNodes = context.GetFragmentSpreads(fragment.SelectionSet);
        if (spreadNodes.Count == 0)
        {
            return;
        }

        spreadPathIndexByName[fragmentName] = spreadPath.Count;

        // Saved parents let us resume siblings and unwind the active path in depth-first order.
        using var stack = new StackSlim<(GraphQLFragmentDefinition fragment, List<GraphQLFragmentSpread> spreads, int spreadIndex)>();
        int spreadIndex = 0;

        while (true)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (spreadIndex == spreadNodes.Count)
            {
                spreadPathIndexByName.Remove(fragment.FragmentName.Name);
                if (!stack.TryPop(out var parent))
                    break;

                spreadPath.TryPop(out _);
                (fragment, spreadNodes, spreadIndex) = parent;
                continue;
            }

            var spreadNode = spreadNodes[spreadIndex++];
            var spreadName = spreadNode.FragmentName.Name;

            // An active ancestor is a cycle even though it has already been marked visited.
            if (spreadPathIndexByName.TryGetValue(spreadName, out var cycleIndex))
            {
                var cyclePath = spreadPath.Reverse().Skip(cycleIndex).ToArray();
                var nodes = cyclePath.OfType<ASTNode>().Concat(new[] { spreadNode }).ToArray();

                context.ReportError(new NoFragmentCyclesError(context, spreadName.StringValue, cyclePath.Select(x => x.FragmentName.Name.StringValue).ToArray(), nodes)); //ISSUE:allocation
                continue;
            }

            if (!unvisitedFragments.TryGetValue(spreadName, out var spreadFragment) || spreadFragment == null)
                continue;

            unvisitedFragments[spreadName] = null;
            var childSpreads = context.GetFragmentSpreads(spreadFragment.SelectionSet);
            if (childSpreads.Count == 0)
                continue;

            stack.Push((fragment, spreadNodes, spreadIndex));
            spreadPath.Push(spreadNode);
            spreadPathIndexByName[spreadName] = spreadPath.Count;
            fragment = spreadFragment;
            spreadNodes = childSpreads;
            spreadIndex = 0;
        }
    }
}
