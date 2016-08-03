using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language.AST
{
    public class Directives : AbstractNode, IEnumerable<Directive>
    {
        private readonly List<Directive> _directives = new List<Directive>();

        public override IEnumerable<INode> Children => _directives;

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

        protected bool Equals(Directives directives)
        {
            return false;
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Directives) obj);
        }
    }
}
