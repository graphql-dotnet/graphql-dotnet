using System;

namespace GraphQL.Language.AST
{
    public class VariableReference : AbstractNode, IValue
    {
        public VariableReference(NameNode name)
        {
            Name = name.Name;
            NameNode = name;
        }

        object IValue.Value => Name;
        public string Name { get; }
        public NameNode NameNode { get; }

        /// <inheritdoc />
        public override string ToString() => $"VariableReference{{name={Name}}}";

        protected bool Equals(VariableReference other)
        {
            return string.Equals(Name, other.Name, StringComparison.InvariantCulture);
        }

        public override bool IsEqualTo(INode obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((VariableReference)obj);
        }
    }
}
