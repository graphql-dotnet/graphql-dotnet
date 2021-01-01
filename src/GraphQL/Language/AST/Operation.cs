using System;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class Operation : AbstractNode, IDefinition, IHaveSelectionSet
    {
        public Operation(NameNode name)
        {
            NameNode = name;
            OperationType = OperationType.Query;
        }

        public string Name => NameNode?.Name;

        public NameNode NameNode { get; }

        public OperationType OperationType { get; set; }

        public Directives Directives { get; set; }

        public VariableDefinitions Variables { get; set; }

        public SelectionSet SelectionSet { get; set; }

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

        /// <inheritdoc />
        public override string ToString() => $"OperationDefinition{{name='{Name}', operation={OperationType}, variableDefinitions={Variables}, directives={Directives}, selectionSet={SelectionSet}}}";

        protected bool Equals(Operation other)
        {
            return string.Equals(Name, other.Name, StringComparison.InvariantCulture) && OperationType == other.OperationType;
        }

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
