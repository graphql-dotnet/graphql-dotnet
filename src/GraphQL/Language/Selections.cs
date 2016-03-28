using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Language
{
    public class Selections : IEnumerable<ISelection>
    {
        private readonly List<ISelection> _selections = new List<ISelection>();

        public void Add(ISelection selection)
        {
            _selections.Add(selection);
        }

        public IEnumerator<ISelection> GetEnumerator()
        {
            return _selections.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
