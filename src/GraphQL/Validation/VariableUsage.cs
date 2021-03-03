using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation
{
    /// <summary>
    /// Represents a variable reference node and the graph type it is referenced to be used for.
    /// </summary>
    public class VariableUsage
    {
        /// <summary>
        /// Returns a variable reference node.
        /// </summary>
        public VariableReference Node { get; }

        /// <summary>
        /// Returns a graph type.
        /// </summary>
        public IGraphType Type { get; }

        /// <summary>
        /// Initializes a new instance with the specified parameters.
        /// </summary>
        /// <param name="node">A variable reference node.</param>
        /// <param name="type">A graph type.</param>
        public VariableUsage(VariableReference node, IGraphType type)
        {
            Node = node;
            Type = type;
        }
    }
}
