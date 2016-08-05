namespace GraphQL.Language.AST
{
    public class GraphQLObjectField : ASTNode
    {
        public override ASTNodeKind Kind
        {
            get
            {
                return ASTNodeKind.ObjectField;
            }
        }

        public GraphQLName Name { get; set; }
        public GraphQLValue Value { get; set; }
    }
}
