using GraphQL.Types;

namespace GraphQL.Utilities.Federation.Types
{
    internal class EntityType : UnionGraphType
    {
        public EntityType()
        {
            Name = "_Entity";
            Type<NeverType>();
        }
    }
}
