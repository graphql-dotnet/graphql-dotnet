using GraphQL.Types;

namespace GraphQL.Federation.Types;

internal class EntityType : UnionGraphType
{
    public EntityType()
    {
        Name = "_Entity";
        Type<NeverType>();
    }
}
