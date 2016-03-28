using System.Collections.Generic;

namespace GraphQL.Language
{
    public class FragmentDefinition : AbstractNode, IDefinition
    {
        public FragmentDefinition()
        {
            Directives = new Directives();
        }

        public string Name { get; set; }

        public NamedType Type { get; set; }

        public Directives Directives { get; set; }

        public Selections Selections { get; set; }

        public override IEnumerable<INode> Children
        {
            get
            {
                yield return Type;

                foreach (var directive in Directives)
                {
                    yield return directive;
                }

                foreach (var selection in Selections)
                {
                    yield return selection;
                }
            }
        }

        public override string ToString()
        {
            return "FragmentDefinition{{name='{0}', typeCondition={1}, directives={2}, selectionSet={3}}}"
                .ToFormat(Name, Type, Directives, Selections);
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
