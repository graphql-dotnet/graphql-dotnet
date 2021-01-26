using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a list of fragment definition nodes within a document.
    /// </summary>
    public class Fragments : IEnumerable<FragmentDefinition>
    {
        internal List<FragmentDefinition> List { get; private set; }

        /// <summary>
        /// Adds a fragment definition node to the list.
        /// </summary>
        public void Add(FragmentDefinition fragment) => (List ??= new List<FragmentDefinition>()).Add(fragment ?? throw new ArgumentNullException(nameof(fragment)));

        /// <summary>
        /// Returns the number of fragment definition nodes in the list.
        /// </summary>
        public int Count => List?.Count ?? 0;

        /// <summary>
        /// Searches the list by name and returns the first matching fragment definition, or <see langword="null"/> if none is found.
        /// </summary>
        public FragmentDefinition FindDefinition(string name)
        {
            // DO NOT USE LINQ ON HOT PATH
            if (List != null)
            {
                foreach (var f in List)
                {
                    if (f.Name == name)
                        return f;
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public IEnumerator<FragmentDefinition> GetEnumerator() => (List ?? Enumerable.Empty<FragmentDefinition>()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
