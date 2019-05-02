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

        public override string ToString()
        {
            return "EnumValue{{name={0}}}".ToFormat(Name);
        }

        protected bool Equals(EnumValue other)
        {
            return string.Equals(Name, other.Name);
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return Equals((EnumValue)obj);
        }
    }
}
