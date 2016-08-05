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
            _definitions.Add(definition);

            if (definition is FragmentDefinition)
            {
                Fragments.Add((FragmentDefinition) definition);
            }
            else if (definition is Operation)
            {
                Operations.Add((Operation) definition);
            }
            else
            {
                throw new ExecutionError("Unhandled document definition");
            }
        }

        public override string ToString()
        {
            return "Document{{definitions={0}}}".ToFormat(_definitions);
        }

        public override bool IsEqualTo(INode node)
        {
            if (ReferenceEquals(null, node)) return false;
            if (ReferenceEquals(this, node)) return true;
            if (node.GetType() != this.GetType()) return false;

            return true;
        }
    }
}
