namespace GraphQL.Language.AST
{
    public class GraphQLTypeExtensionDefinition : GraphQLTypeDefinition
    {
        public GraphQLObjectTypeDefinition Definition { get; set; }

        public override ASTNodeKind Kind
        {
            get
            {
                return ASTNodeKind.TypeExtensionDefinition;
            }
        }
    }
}
