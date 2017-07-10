namespace GraphQL.Types
{
    public class GraphQLTypeReference : InterfaceGraphType
    {
        public GraphQLTypeReference(string typeName)
        {
            Name = "__GraphQLTypeReference";
            TypeName = typeName;
        }

        public string TypeName { get; private set; }
    }
}