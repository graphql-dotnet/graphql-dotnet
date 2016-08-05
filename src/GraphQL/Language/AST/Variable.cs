using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class Variable
    {
        public string Name { get; set; }

        public object Value { get; set; }
    }

    public class VariableDefinition : AbstractNode
    {
        public VariableDefinition()
        {
        }

        public VariableDefinition(NameNode node)
        {
            Name = node.Name;
            NameNode = node;
        }

        public string Name { get; set; }
        public NameNode NameNode { get; set; }
        public IType Type { get; set; }
        public IValue DefaultValue { get; set; }

        public override IEnumerable<INode> Children
        {
            get
            {
                yield return Type;

                if (DefaultValue != null)
                {
                    yield return DefaultValue;
                }
            }
        }

        public override string ToString()
        {
            return "VariableDefinition{{name={0},type={1},defaultValue={2}}}"
                .ToFormat(Name, Type, DefaultValue);
        }

        protected bool Equals(VariableDefinition other)
        {
            return string.Equals(Name, other.Name);
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((VariableDefinition) obj);
        }
    }

    public class VariableReference : AbstractNode, IValue
    {
        public VariableReference(NameNode name)
        {
            Name = name.Name;
            NameNode = name;
        }

        public string Name { get; }
        public NameNode NameNode { get; }

        public override string ToString()
        {
            return "VariableReference{{name={0}}}".ToFormat(Name);
        }

        protected bool Equals(VariableReference other)
        {
            return string.Equals(Name, other.Name);
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((VariableReference) obj);
        }
    }
}
