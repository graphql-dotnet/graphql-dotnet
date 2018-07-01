using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language.AST
{
    public class SelectionSet : AbstractNode
    {
        private SelectionSet(List<ISelection> selections)
        {
            _selections = selections;
        }

        public SelectionSet() {}
        private readonly List<ISelection> _selections = new List<ISelection>();

        public IEnumerable<ISelection> Selections => _selections;
        public override IEnumerable<INode> Children => _selections;

        public void Add(ISelection selection)
        {
            _selections.Add(selection);
        }

        public SelectionSet Merge(SelectionSet otherSelection)
        {
            var newSelection = otherSelection.Selections.Union(_selections).ToList();
            return new SelectionSet(newSelection);
        }

        protected bool Equals(SelectionSet selectionSet)
        {
            return false;
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SelectionSet)obj);
        }

        public override string ToString()
        {
            var sel = string.Join(", ", _selections.Select(s => s.ToString()));
            return $"SelectionSet{{selections={sel}}}";
        }
    }
}
