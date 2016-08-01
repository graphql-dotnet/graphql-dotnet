using System;

namespace GraphQL.Language.AST
{
    public class NameNode : AbstractNode
    {
        public NameNode(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public override bool IsEqualTo(INode node)
        {
            throw new NotImplementedException();
        }
    }
}
