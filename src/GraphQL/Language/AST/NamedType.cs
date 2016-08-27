namespace GraphQL.Language.AST
{
    public class NamedType : AbstractNode, IType
    {
        public NamedType(NameNode node)
            : this(node.Name)
        {
            NameNode = node;
        }

        public NamedType(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public NameNode NameNode { get; }

        public override string ToString()
        {
            return "NamedType{{name={0}}}".ToFormat(Name);
        }

        protected bool Equals(NamedType other)
        {
            return string.Equals(Name, other.Name);
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;

            return Equals((NamedType) obj);
        }
    }
}
