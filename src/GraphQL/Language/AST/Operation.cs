using System;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents an operation within a document.
    /// </summary>
    public class Operation : AbstractNode, IDefinition, IHaveSelectionSet
    {
        /// <summary>
        /// Initializes a new operation node with the specified <see cref="NameNode"/> containing the name of the operation, if any.
        /// </summary>
        public Operation(NameNode name)
        {
            NameNode = name;
            OperationType = OperationType.Query;
        }

        /// <summary>
        /// Returns the name of the operation, if any.
        /// </summary>
        public string Name => NameNode.Name;

        /// <summary>
        /// Returns the <see cref="NameNode"/> containing the name of the operation, if any.
        /// </summary>
        public NameNode NameNode { get; }

        /// <summary>
        /// Gets or sets the type of this operation.
        /// </summary>
        public OperationType OperationType { get; set; }

        /// <summary>
        /// Gets or sets a list of directive nodes for this operation.
        /// </summary>
        public Directives Directives { get; set; }

        /// <summary>
        /// Gets or sets a list of variable definition nodes for this operation.
        /// </summary>
        public VariableDefinitions Variables { get; set; }

        /// <inheritdoc/>
        public SelectionSet SelectionSet { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<INode> Children
        {
            get
            {
                if (Variables != null)
                {
                    foreach (var variable in Variables)
                    {
                        yield return variable;
                    }
                }

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

        /// <inheritdoc/>
        public override string ToString() => $"OperationDefinition{{name='{Name}', operation={OperationType}, variableDefinitions={Variables}, directives={Directives}, selectionSet={SelectionSet}}}";

        /// <summary>
        /// Compares this instance to another instance by name.
        /// </summary>
        protected bool Equals(Operation other)
        {
            return string.Equals(Name, other.Name, StringComparison.InvariantCulture) && OperationType == other.OperationType;
        }

        /// <inheritdoc/>
        public override bool IsEqualTo(INode node)
        {
            if (node is null)
                return false;
            if (ReferenceEquals(this, node))
                return true;
            if (node.GetType() != GetType())
                return false;
            return Equals((Operation)node);
        }
    }
}
