using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language
{
    public class Fragments : IEnumerable<IFragment>
    {
        private readonly List<IFragment> _fragments = new List<IFragment>();

        public void Add(IFragment fragment)
        {
            _fragments.Add(fragment);
        }

        public FragmentDefinition FindDefinition(string name)
        {
            return _fragments.OfType<FragmentDefinition>().FirstOrDefault(f => f.Name == name);
        }

        public IEnumerator<IFragment> GetEnumerator()
        {
            return _fragments.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
