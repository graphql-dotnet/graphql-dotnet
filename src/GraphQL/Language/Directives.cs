using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language
{
    public class Directives : IEnumerable<Directive>
    {
        private readonly List<Directive> _directives = new List<Directive>();

        public void Add(Directive directive)
        {
            _directives.Add(directive);
        }

        public Directive Find(string name)
        {
            return _directives.FirstOrDefault(d => d.Name == name);
        }

        public IEnumerator<Directive> GetEnumerator()
        {
            return _directives.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
