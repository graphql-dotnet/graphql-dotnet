using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation;

public partial class ValidationContext
{
    private Dictionary<ROM, GraphQLFragmentDefinition>? _fragmentDefinitions;

    // Build only when a rule needs a fragment lookup, and keep the first
    // definition for duplicate names, just like Document.FindFragmentDefinition.
    internal GraphQLFragmentDefinition? FindFragmentDefinition(ROM name)
    {
        // Avoid a lookup dictionary for documents with only a few definitions.
        if (Document.Definitions.Count <= 10)
            return Document.FindFragmentDefinition(name);

        if (_fragmentDefinitions == null)
        {
            var fragments = new Dictionary<ROM, GraphQLFragmentDefinition>(Document.FragmentsCount());
            foreach (var definition in Document.Definitions)
            {
                if (definition is GraphQLFragmentDefinition fragment)
                {
#if NETSTANDARD2_0
                    if (!fragments.ContainsKey(fragment.FragmentName.Name))
                        fragments.Add(fragment.FragmentName.Name, fragment);
#else
                    fragments.TryAdd(fragment.FragmentName.Name, fragment);
#endif
                }
            }
            _fragmentDefinitions = fragments;
        }

        return _fragmentDefinitions.TryGetValue(name, out var result) ? result : null;
    }
}

