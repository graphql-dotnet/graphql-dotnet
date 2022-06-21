using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation.Complexity;

/// <summary>
/// Sorts fragment definitions so that independent fragments go in front.
/// This class does not detect cycles; <see cref="Rules.NoFragmentCycles"/> should be used before.
/// </summary>
internal sealed class NestedFragmentsComparer : IComparer<GraphQLFragmentDefinition>
{
    private readonly GraphQLDocument _document;
    private readonly Stack<GraphQLSelectionSet> _selectionSetsToVisit = new();

    public NestedFragmentsComparer(GraphQLDocument document)
    {
        _document = document;
    }

    public int Compare(GraphQLFragmentDefinition? x, GraphQLFragmentDefinition? y)
    {
        bool yInX = Contains(x!, y!, out var xHasSpreads);
        bool xInY = Contains(y!, x!, out var yHasSpreads);

        if (yInX && xInY)
            throw new InvalidOperationException("Fragment cycle detected, NoFragmentCycles validation rule should be used.");

        if (yInX)
            return 1; // 'y' should go first

        if (xInY)
            return -1; // 'x' should go first

        if (xHasSpreads && !yHasSpreads)
            return 1; // 'y' should go first

        if (yHasSpreads && !xHasSpreads)
            return -1; // 'x' should go first

        return 0;
    }

    private bool Contains(GraphQLFragmentDefinition outer, GraphQLFragmentDefinition inner, out bool hasAnySpreads)
    {
        hasAnySpreads = false;
        _selectionSetsToVisit.Clear();
        _selectionSetsToVisit.Push(outer.SelectionSet);

        while (_selectionSetsToVisit.Count > 0)
        {
            foreach (var selection in _selectionSetsToVisit.Pop().Selections)
            {
                if (selection is GraphQLFragmentSpread spread)
                {
                    hasAnySpreads = true;
                    if (spread.FragmentName.Name == inner.FragmentName.Name)
                    {
                        _selectionSetsToVisit.Clear();
                        return true;
                    }
                    else
                    {
                        var frag = _document.FindFragmentDefinition(spread.FragmentName.Name.Value);
                        if (frag != null)
                            _selectionSetsToVisit.Push(frag.SelectionSet);
                    }
                }
                else if (selection is IHasSelectionSetNode hasSet && hasSet.SelectionSet != null)
                {
                    _selectionSetsToVisit.Push(hasSet.SelectionSet);
                }
            }
        }

        _selectionSetsToVisit.Clear();
        return false;
    }
}
