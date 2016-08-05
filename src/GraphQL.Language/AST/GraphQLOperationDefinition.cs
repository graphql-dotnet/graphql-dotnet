using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class GraphQLOperationDefinition : ASTNode
    {
        public IEnumerable<GraphQLDirective> Directives { get; set; }

        public override ASTNodeKind Kind
        {
            get
            {
                return ASTNodeKind.OperationDefinition;
            }
        }

        public GraphQLName Name { get; set; }
        public OperationType Operation { get; set; }
        public GraphQLSelectionSet SelectionSet { get; set; }
        public IEnumerable<GraphQLVariableDefinition> VariableDefinitions { get; set; }
    }
}
