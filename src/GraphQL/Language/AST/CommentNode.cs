using System;

namespace GraphQL.Language.AST
{
    public class CommentNode : AbstractNode
    {
        public CommentNode(string comment)
        {
            Comment = comment;
        }

        public string Comment { get; }

        public override bool IsEqualTo(INode node)
        {
            throw new NotImplementedException();
        }
    }
}
