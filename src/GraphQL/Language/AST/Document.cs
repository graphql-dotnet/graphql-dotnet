using System;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class Document : AbstractNode
    {
        private readonly List<IDefinition> _definitions;

        public Document()
        {
            _definitions = new List<IDefinition>();
            Operations = new Operations();
            Fragments = new Fragments();
        }

        public string OriginalQuery { get; set; }

        public override IEnumerable<INode> Children => _definitions;

        public Operations Operations { get; }

        public Fragments Fragments { get; }

        public void AddDefinition(IDefinition definition)
        {
            _definitions.Add(definition ?? throw new ArgumentNullException(nameof(definition)));

            if (definition is FragmentDefinition fragmentDefinition)
            {
                Fragments.Add(fragmentDefinition);
            }
            else if (definition is Operation operation)
            {
                Operations.Add(operation);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(definition), $"Unhandled document definition '{definition.GetType().Name}'");
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"Document{{definitions={string.Join(", ", _definitions)}}}";

        public override bool IsEqualTo(INode node)
        {
            if (node is null)
                return false;
            if (ReferenceEquals(this, node))
                return true;
            if (node.GetType() != GetType())
                return false;

            return true;
        }
    }
}
