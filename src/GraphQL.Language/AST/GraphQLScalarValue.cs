namespace GraphQL.Language.AST
{
    public class GraphQLScalarValue : GraphQLValue
    {
        private ASTNodeKind kindField;

        public GraphQLScalarValue(ASTNodeKind kind)
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

        public string Value { get; set; }

        public override string ToString()
        {
            if (this.Kind == ASTNodeKind.StringValue)
                return $"\"{this.Value}\"";

            return this.Value.ToString();
        }
    }
}
