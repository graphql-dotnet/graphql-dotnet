using System;
using GraphQL.SchemaGenerator.Schema;

namespace GraphQL.SchemaGenerator.Attributes
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
    public class GraphKnownTypeAttribute : Attribute, IDomainSchemaTypeMapping
    {
        public Type DomainType { get; set; }
        public Type SchemaType { get; set; }

        public GraphKnownTypeAttribute(Type domainType, Type schemaType)
        {
            DomainType = domainType;
            SchemaType = schemaType;
        }
    }
}
