using System.Threading.Tasks;
using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    internal class NullNodeVisitor : INodeVisitor
    {
        private NullNodeVisitor() { }

        public static readonly NullNodeVisitor Instance = new NullNodeVisitor();
        public static readonly Task<INodeVisitor> TaskInstance = Task.FromResult((INodeVisitor)Instance);

        void INodeVisitor.Enter(INode node, ValidationContext context) { }

        void INodeVisitor.Leave(INode node, ValidationContext context) { }
    }
}
