namespace GraphQL.Language.AST
{
    public class GraphQLListType : GraphQLType
    {
        public override ASTNodeKind Kind
        {
            get
            {
                return ASTNodeKind.ListType;
            }
        }

        public GraphQLType Type { get; set; }
    }
}
