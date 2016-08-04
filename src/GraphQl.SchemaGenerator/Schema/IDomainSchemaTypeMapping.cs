using System;

namespace GraphQl.SchemaGenerator.Schema
{
    public interface IDomainSchemaTypeMapping
    {
        Type DomainType { get; }
        Type SchemaType { get; }
    }
}
