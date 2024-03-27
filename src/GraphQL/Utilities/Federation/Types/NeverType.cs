using GraphQL.Types;

namespace GraphQL.Utilities.Federation.Types
{
    internal class NeverType : ObjectGraphType
    {
        public NeverType()
        {
            Name = "_Never";
            IsTypeOf = _ => false;
            Field<NeverType>("_never");
        }
    }
}
