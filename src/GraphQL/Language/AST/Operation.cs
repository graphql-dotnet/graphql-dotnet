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

        public string Comment => CommentNode?.Comment;
        public CommentNode CommentNode { get; set; }

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

        public override string ToString()
        {
            return "OperationDefinition{{name='{0}', operation={1}, variableDefinitions={2}, directives={3}, selectionSet={4}}}"
                .ToFormat(Name, OperationType, Variables, Directives, SelectionSet);
        }

        protected bool Equals(Operation other)
        {
            return string.Equals(Name, other.Name, StringComparison.InvariantCulture) && OperationType == other.OperationType;
        }

        public override bool IsEqualTo(INode node)
        {
            if (node is null) return false;
            if (ReferenceEquals(this, node)) return true;
            if (node.GetType() != GetType()) return false;
            return Equals((Operation)node);
        }
    }
}
