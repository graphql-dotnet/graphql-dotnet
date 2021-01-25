using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a list of field nodes or fragment nodes selected to be returned.
    /// </summary>
    public class SelectionSet : AbstractNode
    {
        /// <summary>
        /// Initializes a new instance with an empty list.
        /// </summary>
        public SelectionSet()
        {
            SelectionsList = new List<ISelection>();
        }

        private SelectionSet(List<ISelection> selections)
        {
            SelectionsList = selections;
        }

        //TODO: change to List<> ?
        /// <summary>
        /// Returns the list of selected nodes.
        /// </summary>
        public IList<ISelection> Selections => SelectionsList;

        // avoids List+Enumerator<ISelection> boxing
        internal List<ISelection> SelectionsList { get; private set; }

        /// <inheritdoc/>
        public override IEnumerable<INode> Children => SelectionsList;

        /// <inheritdoc/>
        public override void Visit<TState>(Action<INode, TState> action, TState state)
        {
            if (SelectionsList != null)
            {
                foreach (var selection in SelectionsList)
                    action(selection, state);
            }
        }

        /// <summary>
        /// Adds a node to the start of the list.
        /// </summary>
        public void Prepend(ISelection selection)
        {
            SelectionsList.Insert(0, selection ?? throw new ArgumentNullException(nameof(selection)));
        }

        /// <summary>
        /// Adds a node to the end of the list.
        /// </summary>
        public void Add(ISelection selection)
        {
            SelectionsList.Add(selection ?? throw new ArgumentNullException(nameof(selection)));
        }

        /// <summary>
        /// Returns a new selection set node with the contents merged with another selection set node's contents.
        /// </summary>
        public SelectionSet Merge(SelectionSet otherSelection)
        {
            var newSelection = SelectionsList.Union(otherSelection.SelectionsList).ToList();
            return new SelectionSet(newSelection);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string sel = string.Join(", ", SelectionsList.Select(s => s.ToString()));
            return $"SelectionSet{{selections={sel}}}";
        }
    }
}
