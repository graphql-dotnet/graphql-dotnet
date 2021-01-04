using System;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a field selection node of a document.
    /// </summary>
    public class Field : AbstractNode, ISelection, IHaveSelectionSet
    {
        /// <summary>
        /// Initializes a new instance of a field selection node.
        /// </summary>
        public Field()
        {
        }

        /// <summary>
        /// Initializes a new instance of a field selection node with the specified parameters.
        /// </summary>
        public Field(NameNode alias, NameNode name)
        {
            Alias = alias?.Name;
            AliasNode = alias;
            NameNode = name;
        }

        /// <summary>
        /// Returns the name of the field.
        /// </summary>
        public string Name => NameNode?.Name;

        /// <summary>
        /// Returns the <see cref="NameNode"/> containing the name of this field.
        /// </summary>
        public NameNode NameNode { get; }

        /// <summary>
        /// Returns the alias for this field, if any.
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Returns the <see cref="NameNode"/> containing the alias of this field, if any.
        /// </summary>
        public NameNode AliasNode { get; }

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

        /// <summary>
        /// Determines if this instance is equal to another instance by comparing the <see cref="Name"/> and <see cref="Alias"/> properties.
        /// </summary>
        protected bool Equals(Field other)
        {
            return string.Equals(Name, other.Name, StringComparison.InvariantCulture) && string.Equals(Alias, other.Alias, StringComparison.InvariantCulture);
        }

        /// <inheritdoc/>
        public override bool IsEqualTo(INode obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((Field)obj);
        }

        /// <summary>
        /// Returns a new field selection node with the child field selection nodes merged with another field's child field selection nodes.
        /// </summary>
        public Field MergeSelectionSet(Field other)
        {
            if (Equals(other))
            {
                return new Field(AliasNode, NameNode)
                {
                    Arguments = Arguments,
                    SelectionSet = SelectionSet.Merge(other.SelectionSet),
                    Directives = Directives,
                    SourceLocation = SourceLocation,
                };
            }
            return this;
        }
    }
}
