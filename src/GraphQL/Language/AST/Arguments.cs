using System;
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
            _arguments.Add(arg ?? throw new ArgumentNullException(nameof(arg)));
        }

        public IValue ValueFor(string name)
        {
            // DO NOT USE LINQ ON HOT PATH
            foreach (var x in _arguments)
                if (x.Name == name)
                    return x.Value;

            return null;
        }

        protected bool Equals(Arguments args) => false;

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Arguments)obj);
        }

        public IEnumerator<Argument> GetEnumerator() => _arguments.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
