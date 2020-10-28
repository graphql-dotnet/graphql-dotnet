using System;
using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class Directives : AbstractNode, ICollection<Directive>
    {
        private List<Directive> _directives;
        private readonly Dictionary<string, Directive> _unique = new Dictionary<string, Directive>(StringComparer.Ordinal);

        public override IEnumerable<INode> Children => _directives;

        public void Add(Directive directive)
        {
            if (directive == null)
                throw new ArgumentNullException(nameof(directive));

            if (_directives == null)
                _directives = new List<Directive>();

            _directives.Add(directive);

            if (!_unique.ContainsKey(directive.Name))
            {
                _unique.Add(directive.Name, directive);
            }
        }

        public Directive Find(string name) => _unique.TryGetValue(name, out Directive value) ? value : null;

        public int Count => _directives?.Count ?? 0;

        public bool HasDuplicates => _directives?.Count != _unique.Count;

        public bool IsReadOnly => false;

        public IEnumerator<Directive> GetEnumerator()
        {
            if (_directives == null)
                return System.Linq.Enumerable.Empty<Directive>().GetEnumerator();

            return _directives.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override bool IsEqualTo(INode obj) => ReferenceEquals(this, obj);

        public void Clear()
        {
            _directives.Clear();
            _unique.Clear();
        }

        public bool Contains(Directive item) => _directives.Contains(item);

        public void CopyTo(Directive[] array, int arrayIndex) => _directives.CopyTo(array, arrayIndex);

        public bool Remove(Directive item) => _directives.Remove(item) && _unique.Remove(item.Name);
    }
}
