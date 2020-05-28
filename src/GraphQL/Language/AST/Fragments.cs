using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language.AST
{
    public class Fragments : IEnumerable<FragmentDefinition>
    {
        private readonly List<FragmentDefinition> _fragments = new List<FragmentDefinition>();

        public void Add(FragmentDefinition fragment)
        {
            _fragments.Add(fragment ?? throw new ArgumentNullException(nameof(fragment)));
        }

        public FragmentDefinition FindDefinition(string name)
        {
            // DO NOT USE LINQ ON HOT PATH
            foreach (var f in _fragments)
                if (f.Name == name)
                    return f;

            return null;
        }

        public IEnumerator<FragmentDefinition> GetEnumerator()
        {
            return _fragments.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
