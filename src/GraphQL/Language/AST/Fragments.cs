using System;
using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a list of fragment definition nodes within a document.
    /// </summary>
    public class Fragments : IEnumerable<FragmentDefinition>
    {
        private readonly List<FragmentDefinition> _fragments = new List<FragmentDefinition>();

        /// <summary>
        /// Adds a fragment definition node to the list.
        /// </summary>
        public void Add(FragmentDefinition fragment)
        {
            _fragments.Add(fragment ?? throw new ArgumentNullException(nameof(fragment)));
        }

        /// <summary>
        /// Returns the number of fragment definition nodes in the list.
        /// </summary>
        public int Count => _fragments.Count;

        /// <summary>
        /// Searches the list by name and returns the first matching fragment definition, or <see langword="null"/> if none is found.
        /// </summary>
        public FragmentDefinition FindDefinition(string name)
        {
            // DO NOT USE LINQ ON HOT PATH
            foreach (var f in _fragments)
                if (f.Name == name)
                    return f;

            return null;
        }

        /// <inheritdoc/>
        public IEnumerator<FragmentDefinition> GetEnumerator()
        {
            return _fragments.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
