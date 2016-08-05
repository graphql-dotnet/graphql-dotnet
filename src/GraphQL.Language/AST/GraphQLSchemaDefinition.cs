using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class GraphQLSchemaDefinition : ASTNode
    {
        public IEnumerable<GraphQLDirective> Directives { get; set; }

        public override ASTNodeKind Kind
        {
            get
            {
                return ASTNodeKind.SchemaDefinition;
            }
        }

        public IEnumerable<GraphQLOperationTypeDefinition> OperationTypes { get; set; }
    }
}
