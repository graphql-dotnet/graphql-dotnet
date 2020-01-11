using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language.AST
{
    public class SelectionSet : AbstractNode
    {
        public SelectionSet()
        {
            SelectionsList = new List<ISelection>();
        }

        private SelectionSet(List<ISelection> selections)
        {
            SelectionsList = selections;
        }

        //TODO: change to List<> ?
        public IList<ISelection> Selections => SelectionsList;

        // avoids List+Enumerator<ISelection> boxing on hot path
        internal List<ISelection> SelectionsList { get; }

        public override IEnumerable<INode> Children => SelectionsList;

        public void Prepend(ISelection selection)
        {
            SelectionsList.Insert(0, selection ?? throw new ArgumentNullException(nameof(selection)));
        }

        public void Add(ISelection selection)
        {
            SelectionsList.Add(selection ?? throw new ArgumentNullException(nameof(selection)));
        }

        public SelectionSet Merge(SelectionSet otherSelection)
        {
            var newSelection = SelectionsList.Union(otherSelection.SelectionsList).ToList();
            return new SelectionSet(newSelection);
        }

        protected bool Equals(SelectionSet selectionSet) => false;

        public override bool IsEqualTo(INode obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((SelectionSet)obj);
        }

        public override string ToString()
        {
            var sel = string.Join(", ", SelectionsList.Select(s => s.ToString()));
            return $"SelectionSet{{selections={sel}}}";
        }
    }
}
