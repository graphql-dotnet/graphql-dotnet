using GraphQL.Language;

namespace GraphQL.Validation
{
    public class DebugNodeVisitor : INodeVisitor
    {
        public void Enter(INode node)
        {
            System.Diagnostics.Debug.WriteLine($"Entering {node}");
        }

        public void Leave(INode node)
        {
            System.Diagnostics.Debug.WriteLine($"Leaving {node}");
        }
    }
}
