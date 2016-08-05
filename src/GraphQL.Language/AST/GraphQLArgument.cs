namespace GraphQL.Language.AST
{
    public class GraphQLArgument : ASTNode
    {
        public override ASTNodeKind Kind
        {
            get
            {
                return ASTNodeKind.Argument;
            }
        }

        public GraphQLName Name { get; set; }
        public GraphQLValue Value { get; set; }
    }
}
