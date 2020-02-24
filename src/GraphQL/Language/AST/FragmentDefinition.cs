using System;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class FragmentDefinition : AbstractNode, IDefinition, IHaveSelectionSet
    {
        public FragmentDefinition(NameNode node)
        {
            NameNode = node;
        }

        public string Name => NameNode?.Name;

        public NameNode NameNode { get; }

        public NamedType Type { get; set; }

        public Directives Directives { get; set; }

        public SelectionSet SelectionSet { get; set; }

        public override IEnumerable<INode> Children
        {
            get
            {
                yield return Type;

                if (Directives != null)
                {
                    foreach (var directive in Directives)
                    {
                        yield return directive;
                    }
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
            return string.Equals(Name, other.Name, StringComparison.InvariantCulture);
        }

        public override bool IsEqualTo(INode obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((FragmentDefinition)obj);
        }
    }
}
