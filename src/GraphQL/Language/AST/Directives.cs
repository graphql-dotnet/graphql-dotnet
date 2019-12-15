using GraphQL.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class Directives : AbstractNode, IEnumerable<Directive>
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

        public IEnumerator<Directive> GetEnumerator()
        {
            if (_directives == null)
                return EmptyEnumerator<Directive>.Instance;

            return _directives.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        protected bool Equals(Directives directives) => false;

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Directives)obj);
        }
    }
}
