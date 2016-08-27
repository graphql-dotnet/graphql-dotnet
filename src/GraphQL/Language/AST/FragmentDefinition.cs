using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class FragmentDefinition : AbstractNode, IDefinition, IHaveSelectionSet
    {
        public FragmentDefinition(NameNode node)
            : this()
        {
            Name = node.Name;
            NameNode = node;
        }

        public FragmentDefinition()
        {
            Directives = new Directives();
        }

        public string Name { get; set; }

        public NameNode NameNode { get; }

        public NamedType Type { get; set; }

        public Directives Directives { get; set; }

        public SelectionSet SelectionSet { get; set; }

        public override IEnumerable<INode> Children
        {
            get
            {
                yield return Type;

                foreach (var directive in Directives)
                {
                    yield return directive;
                }

                yield return SelectionSet;
            }
        }

        public override string ToString()
        {
            return "FragmentDefinition{{name='{0}', typeCondition={1}, directives={2}, selectionSet={3}}}"
                .ToFormat(Name, Type, Directives, SelectionSet);
        }

        protected bool Equals(FragmentDefinition other)
        {
            return string.Equals(Name, other.Name);
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FragmentDefinition) obj);
        }
    }
}
