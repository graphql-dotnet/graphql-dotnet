using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Language
{
    public class Selections : IEnumerable<Selection>
    {
        private readonly List<Selection> _selections = new List<Selection>();

        public void Add(Selection selection)
        {
            _selections.Add(selection);
        }

        public IEnumerator<Selection> GetEnumerator()
        {
            return _selections.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}