using GraphQL.Types;

namespace GraphQL.Utilities.Federation
{
    /// <summary>
    /// A union of all types that use the @key directive, including
    /// both types native to the schema and extended types.
    /// <br/>
    /// <see href="https://www.apollographql.com/docs/federation/federation-spec/#union-_entity"/>
    /// </summary>
    public class EntityGraphType : UnionGraphType
    {
        public EntityGraphType()
        {
            Name = "_Entity";
            Description = "A union of all types that use the @key directive";
        }
    }
}
