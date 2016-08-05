using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class GraphQLInputValueDefinition : GraphQLTypeDefinition
    {
        public GraphQLValue DefaultValue { get; set; }

        public IEnumerable<GraphQLDirective> Directives { get; set; }

        public override ASTNodeKind Kind
        {
            get
            {
                return ASTNodeKind.InputValueDefinition;
            }
        }

        public GraphQLName Name { get; set; }
        public GraphQLType Type { get; set; }
    }
}
