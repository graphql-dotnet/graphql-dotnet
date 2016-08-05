using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class GraphQLInterfaceTypeDefinition : GraphQLTypeDefinition
    {
        public IEnumerable<GraphQLDirective> Directives { get; set; }

        public IEnumerable<GraphQLFieldDefinition> Fields { get; set; }

        public override ASTNodeKind Kind
        {
            get
            {
                return ASTNodeKind.InterfaceTypeDefinition;
            }
        }

        public GraphQLName Name { get; set; }
    }
}
