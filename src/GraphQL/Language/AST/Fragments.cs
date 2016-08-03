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
            _fragments.Add(fragment);
        }

        public FragmentDefinition FindDefinition(string name)
        {
            return _fragments.FirstOrDefault(f => f.Name == name);
        }

        public IEnumerator<FragmentDefinition> GetEnumerator()
        {
            return _fragments.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
