using System;
using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class Arguments : AbstractNode, IEnumerable<Argument>
    {
        private List<Argument> _arguments;

        public override IEnumerable<INode> Children => _arguments;

        public void Add(Argument arg)
        {
            if (arg == null)
                throw new ArgumentNullException(nameof(arg));

            if (_arguments == null)
                _arguments = new List<Argument>();

            _arguments.Add(arg);
        }

        public IValue ValueFor(string name)
        {
            if (_arguments == null)
                return null;

            // DO NOT USE LINQ ON HOT PATH
            foreach (var x in _arguments)
                if (x.Name == name)
                    return x.Value;

            return null;
        }

        protected bool Equals(Arguments args) => false;

        public override bool IsEqualTo(INode obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Arguments)obj);
        }

        public IEnumerator<Argument> GetEnumerator()
        {
            if (_arguments == null)
                return System.Linq.Enumerable.Empty<Argument>().GetEnumerator();

            return _arguments.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
