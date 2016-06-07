using System.Collections.Generic;

namespace GraphQL.Language
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

        public void AddDefinition(IDefinition definition)
        {
            _definitions.Add(definition);
        }

        public override IEnumerable<INode> Children => _definitions;

        public Operations Operations { get; }

        public Fragments Fragments { get; }

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
