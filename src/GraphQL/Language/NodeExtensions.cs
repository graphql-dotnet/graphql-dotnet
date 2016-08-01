namespace GraphQL.Language
{
    public static class NodeExtensions
    {
        public static T WithLocation<T>(this T node, Position position)
            where T : AbstractNode
        {
            return node.WithLocation(position.Line, position.Column - 1);
        }

        public static T WithLocation<T>(this T node, int line, int column)
            where T : AbstractNode
        {
            node.SourceLocation = new SourceLocation(line, column);
            return node;
        }
    }
}
