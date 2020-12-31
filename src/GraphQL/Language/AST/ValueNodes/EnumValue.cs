using System;

namespace GraphQL.Language.AST
{
    public class EnumValue : AbstractNode, IValue
    {
        public EnumValue(NameNode name)
        {
            Name = name.Name;
            NameNode = name;
        }

        public EnumValue(string name)
        {
            Name = name;
        }

        object IValue.Value => Name;

        public string Name { get; }

        public NameNode NameNode { get; }

        /// <inheritdoc />
        public override string ToString() => $"EnumValue{{name={Name}}}";

        protected bool Equals(EnumValue other) => string.Equals(Name, other.Name, StringComparison.InvariantCulture);

        public override bool IsEqualTo(INode obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;

            return Equals((EnumValue)obj);
        }
    }
}
