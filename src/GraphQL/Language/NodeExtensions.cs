using GraphQL.Language.AST;

namespace GraphQL.Language
{
    public static class NodeExtensions
    {
        public static T WithLocation<T>(this T node, int line, int column, int start = -1, int end = -1)
            where T : AbstractNode
        {
            node.SourceLocation = new SourceLocation(line, column, start, end);
            return node;
        }
    }
}
