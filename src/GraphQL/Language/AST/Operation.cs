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
                    yield return Directives;
                }

                yield return SelectionSet;
            }
        }

        /// <inheritdoc/>
        public override void Visit<TState>(Action<INode, TState> action, TState state)
        {
            var variables = Variables?.List;
            if (variables != null)
            {
                foreach (var variable in variables)
                    action(variable, state);
            }

            action(Directives, state);
            action(SelectionSet, state);
        }

        /// <inheritdoc/>
        public override string ToString() => $"OperationDefinition{{name='{Name}', operation={OperationType}, variableDefinitions={Variables}, directives={Directives}, selectionSet={SelectionSet}}}";
    }
}
