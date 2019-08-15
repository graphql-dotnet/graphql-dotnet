using System;
using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class Directives : AbstractNode, IEnumerable<Directive>
    {
        private readonly List<Directive> _directives = new List<Directive>();
        private readonly Dictionary<string, Directive> _unique = new Dictionary<string, Directive>(StringComparer.Ordinal);

        public override IEnumerable<INode> Children => _directives;

        public void Add(Directive directive)
        {
            _directives.Add(directive ?? throw new ArgumentNullException(nameof(directive)));

            if (!_unique.ContainsKey(directive.Name))
            {
                _unique.Add(directive.Name, directive);
            }
        }

        public Directive Find(string name)
        {
            _unique.TryGetValue(name, out Directive value);
            return value;
        }

        public int Count => _directives.Count;

        public bool HasDuplicates => _directives.Count != _unique.Count;

        public IEnumerator<Directive> GetEnumerator()
        {
            return _directives.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected bool Equals(Directives directives)
        {
            return false;
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Directives)obj);
        }
    }
}
