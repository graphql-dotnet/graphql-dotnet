using GraphQL.Types;

namespace GraphQL.Federation.Types;

internal class EntityGraphType : UnionGraphType
{
    public EntityGraphType()
    {
        Name = "_Entity";
    }
}
