using System.Collections.Generic;

namespace GraphQL.Language
{
    public class Field : AbstractNode, ISelection
    {
        public string Name { get; set; }

        public string Alias { get; set; }

        public Directives Directives { get; set; }

        public Arguments Arguments { get; set; }

        public SelectionSet SelectionSet { get; set; }

        public override IEnumerable<INode> Children
        {
            get
            {
                if (Arguments != null)
                {
                    foreach (var argument in Arguments)
                    {
                        yield return argument;
                    }
                }

                if (Directives != null)
                {
                    foreach (var directive in Directives)
                    {
                        yield return directive;
                    }
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
