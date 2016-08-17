using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class GraphQLListValue : GraphQLValue
    {
        private ASTNodeKind kindField;

        public GraphQLListValue(ASTNodeKind kind)
        {
            this.kindField = kind;
        }

        public string AstValue { get; set; }

        public override ASTNodeKind Kind
        {
            get
            {
                return this.kindField;
            }
        }

        public IEnumerable<GraphQLValue> Values { get; set; }

        public override string ToString()
        {
            return this.AstValue;
        }
    }
}
