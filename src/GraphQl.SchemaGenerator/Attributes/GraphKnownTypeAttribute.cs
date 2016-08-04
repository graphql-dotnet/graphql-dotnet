using System;
using GraphQl.SchemaGenerator.Schema;

namespace GraphQl.SchemaGenerator.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
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
