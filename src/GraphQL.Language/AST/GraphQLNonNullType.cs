namespace GraphQL.Language.AST
{
    public class GraphQLNonNullType : GraphQLType
    {
        public override ASTNodeKind Kind
        {
            get
            {
                return ASTNodeKind.NonNullType;
            }
        }

        public GraphQLType Type { get; set; }

        public override string ToString()
        {
            return this.Type.ToString() + "!";
        }
    }
}
