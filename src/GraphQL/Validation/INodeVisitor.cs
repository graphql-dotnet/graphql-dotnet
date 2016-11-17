using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    public interface INodeVisitor
    {
        void Enter(INode node);
        void Leave(INode node);
    }
}
