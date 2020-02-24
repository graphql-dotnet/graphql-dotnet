using System;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class ObjectField : AbstractNode
    {
        public ObjectField(NameNode name, IValue value)
            : this(name.Name, value)
        {
            NameNode = name;
        }

        public ObjectField(string name, IValue value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public NameNode NameNode { get; }
        public IValue Value { get; }

        public override IEnumerable<INode> Children
        {
            get { yield return Value; }
        }

        public override string ToString()
        {
            return "ObjectField{{name='{0}', value={1}}}".ToFormat(Name, Value);
        }

        protected bool Equals(ObjectField other) => string.Equals(Name, other.Name, StringComparison.InvariantCulture);

        public override bool IsEqualTo(INode obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return Equals((ObjectField)obj);
        }
    }
}
