using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class Field : AbstractNode, ISelection, IHaveSelectionSet
    {
        public Field()
        {
        }

        public Field(NameNode alias, NameNode name)
        {
            Alias = alias?.Name;
            AliasNode = alias;
            Name = name.Name;
            NameNode = name;
        }

        public string Name { get; set; }
        public NameNode NameNode { get; }

        public string Alias { get; set; }
        public NameNode AliasNode { get; }

        public Directives Directives { get; set; }

        public Arguments Arguments { get; set; }

        public SelectionSet SelectionSet { get; set; }

        public override IEnumerable<INode> Children
        {
            get
            {
                if (Arguments != null)
                {
                    yield return Arguments;
                }

                if (Directives != null)
                {
                    yield return Directives;
                }

                if (SelectionSet != null)
                {
                    yield return SelectionSet;
                }
            }
        }

        public override string ToString()
        {
            return "Field{{name='{0}', alias='{1}', arguments={2}, directives={3}, selectionSet={4}}}"
                .ToFormat(Name, Alias, Arguments, Directives, SelectionSet);
        }

        protected bool Equals(Field other)
        {
            return string.Equals(Name, other.Name) && string.Equals(Alias, other.Alias);
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Field) obj);
        }
    }
}
