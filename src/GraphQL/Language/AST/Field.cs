using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a field selection node of a document.
    /// </summary>
    public class Field : AbstractNode, ISelection, IHaveSelectionSet
    {
        /// <summary>
        /// Initializes a new instance of a field selection node with the specified parameters.
        /// </summary>
        public Field(string alias, string name)
        {
            Alias = alias;
            Name = name;
        }

        /// <summary>
        /// Returns the name of the field.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns the alias for this field, if any.
        /// </summary>
        public string Alias { get; }

        /// <summary>
        /// Gets or sets a list of directive nodes for this field selection node.
        /// </summary>
        public Directives Directives { get; set; }

        /// <summary>
        /// Gets or sets a list of argument nodes for this field selection node.
        /// </summary>
        public Arguments Arguments { get; set; }

        /// <inheritdoc/>
        public SelectionSet SelectionSet { get; set; }

        /// <summary>
        /// Returns the argument nodes, directive nodes, and child fields selection nodes contained within this field selection node.
        /// </summary>
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

        /// <inheritdoc />
        public override string ToString() => $"Field{{name='{Name}', alias='{Alias}', arguments={Arguments}, directives={Directives}, selectionSet={SelectionSet}}}";
    }
}
