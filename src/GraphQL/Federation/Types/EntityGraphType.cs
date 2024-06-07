using GraphQL.Types;

namespace GraphQL.Federation.Types;

public class EntityGraphType : UnionGraphType
{
    public EntityGraphType()
    {
        Name = "_Entity";
    }
}
