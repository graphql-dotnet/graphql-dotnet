using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents an operation within a document.
    /// </summary>
    public class Operation : AbstractNode, IDefinition, IHaveSelectionSet
    {
        /// <summary>
        /// Initializes a new operation node with the specified name of the operation, if any.
        /// </summary>
        public Operation(string name)
        {
            Name = name;
            OperationType = OperationType.Query;
        }

        /// <summary>
        /// Returns the name of the operation, if any.
        /// </summary>
        public string Name { get; }

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
    }
}
