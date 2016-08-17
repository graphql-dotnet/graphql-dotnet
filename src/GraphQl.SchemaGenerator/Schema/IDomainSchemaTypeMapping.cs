using System;

namespace GraphQL.SchemaGenerator.Schema
{
    public interface IDomainSchemaTypeMapping
    {
        Type DomainType { get; }
        Type SchemaType { get; }
    }
}
