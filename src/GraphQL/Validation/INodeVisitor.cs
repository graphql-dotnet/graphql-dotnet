using GraphQL.Language;

namespace GraphQL.Validation
{
    public interface INodeVisitor
    {
        void Enter(INode node);
        void Leave(INode node);
    }
}
