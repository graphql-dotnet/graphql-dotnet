using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class GraphQLInputObjectTypeDefinition : GraphQLTypeDefinition
    {
        public IEnumerable<GraphQLDirective> Directives { get; set; }

        public IEnumerable<GraphQLInputValueDefinition> Fields { get; set; }

        public override ASTNodeKind Kind
        {
            get
            {
                return ASTNodeKind.InputObjectTypeDefinition;
            }
        }

        public GraphQLName Name { get; set; }
    }
}
