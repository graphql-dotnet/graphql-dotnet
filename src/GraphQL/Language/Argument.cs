using System.Collections.Generic;

namespace GraphQL.Language
{
    public class Argument : AbstractNode
    {
        public string Name { get; set; }
        public IValue Value { get; set; }

        public override IEnumerable<INode> Children
        {
            get { yield return Value; }
        }

        public override string ToString()
        {
            return "Argument{{name={0},value={1}}}".ToFormat(Name, Value);
        }

        protected bool Equals(Argument other)
        {
            return base.Equals(other) && string.Equals(Name, other.Name);
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Argument) obj);
        }
    }
}
