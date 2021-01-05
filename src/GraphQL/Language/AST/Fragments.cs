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
        private List<FragmentDefinition> _fragments;

        /// <summary>
        /// Adds a fragment definition node to the list.
        /// </summary>
        public void Add(FragmentDefinition fragment)
        {
            (_fragments ??= new List<FragmentDefinition>()).Add(fragment ?? throw new ArgumentNullException(nameof(fragment)));
        }

        /// <summary>
        /// Gets count of fragment definition nodes within a document.
        /// </summary>
        public int Count => _fragments?.Count ?? 0;

        /// <summary>
        /// Searches the list by name and returns the first matching fragment definition, or <see langword="null"/> if none is found.
        /// </summary>
        public FragmentDefinition FindDefinition(string name)
        {
            // DO NOT USE LINQ ON HOT PATH
            if (_fragments != null)
            {
                foreach (var f in _fragments)
                {
                    if (f.Name == name)
                        return f;
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public IEnumerator<FragmentDefinition> GetEnumerator() => _fragments?.GetEnumerator() ?? Enumerable.Empty<FragmentDefinition>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
