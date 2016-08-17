namespace GraphQL.Language.AST
{
    public class GraphQLNamedType : GraphQLType
    {
        public override ASTNodeKind Kind
        {
            get
            {
                return ASTNodeKind.NamedType;
            }
        }

        public GraphQLName Name { get; set; }

        public override string ToString()
        {
            return this.Name.Value;
        }
    }
}
