namespace GraphQL.Language.AST
{
    public class GraphQLValue<T> : GraphQLValue
    {
        private ASTNodeKind kindField;

        public GraphQLValue(ASTNodeKind kind)
        {
            this.kindField = kind;
        }

        public override ASTNodeKind Kind
        {
            get
            {
                return this.kindField;
            }
        }

        public T Value { get; set; }
    }
}
