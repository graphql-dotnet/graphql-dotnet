using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language.AST
{
    public class Arguments : AbstractNode, IEnumerable<Argument>
    {
        private readonly List<Argument> _arguments = new List<Argument>();

        public override IEnumerable<INode> Children => _arguments;

        public void Add(Argument arg)
        {
            _arguments.Add(arg);
        }

        public IValue ValueFor(string name)
        {
            var arg = _arguments.FirstOrDefault(x => x.Name == name);
            return arg != null ? arg.Value : null;
        }

        protected bool Equals(Arguments args)
        {
            return false;
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Arguments) obj);
        }

        public IEnumerator<Argument> GetEnumerator()
        {
            return _arguments.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
