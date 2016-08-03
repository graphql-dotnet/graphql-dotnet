using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class Operation : AbstractNode, IDefinition, IHaveSelectionSet
    {
        public Operation(NameNode name)
            : this()
        {
            Name = name?.Name;
            NameNode = name;
        }

        public Operation()
        {
            OperationType = OperationType.Query;
            Directives = new Directives();
            Variables = new VariableDefinitions();
        }

        public string Name { get; set; }
        public NameNode NameNode { get; }

        public OperationType OperationType { get; set; }

        public Directives Directives { get; set; }

        public VariableDefinitions Variables { get; set; }

        public SelectionSet SelectionSet { get; set; }

        public override IEnumerable<INode> Children
        {
            get
            {
                foreach (var variable in Variables)
                {
                    yield return variable;
                }

                foreach (var directive in Directives)
                {
                    yield return directive;
                }

                yield return SelectionSet;
            }
        }

        public override string ToString()
        {
            return "OperationDefinition{{name='{0}', operation={1}, variableDefinitions={2}, directives={3}, selectionSet={4}}}"
                .ToFormat(Name, OperationType, Variables, Directives, SelectionSet);
        }

        protected bool Equals(Operation other)
        {
            return string.Equals(Name, other.Name) && OperationType == other.OperationType;
        }

        public override bool IsEqualTo(INode node)
        {
            if (ReferenceEquals(null, node)) return false;
            if (ReferenceEquals(this, node)) return true;
            if (node.GetType() != this.GetType()) return false;
            return Equals((Operation) node);
        }
    }
}
