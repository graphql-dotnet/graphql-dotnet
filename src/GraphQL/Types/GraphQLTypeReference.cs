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

        public override bool Equals(object obj)
        {
            if (obj is GraphQLTypeReference other)
            {
                return TypeName == other.TypeName;
            }
            return base.Equals(obj);
        }
    }
}
