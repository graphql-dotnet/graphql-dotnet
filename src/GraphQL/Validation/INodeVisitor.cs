using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    public interface INodeVisitor
    {
        void Enter(INode node, ValidationContext context);

        void Leave(INode node, ValidationContext context);
    }
}
