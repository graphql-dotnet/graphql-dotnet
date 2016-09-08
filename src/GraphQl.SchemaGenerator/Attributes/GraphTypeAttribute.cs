using System;

namespace GraphQL.SchemaGenerator.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field)]
    public class GraphTypeAttribute : Attribute
    {
        public Type Type { get; set; }

        public bool Exclude { get; set; }

        public GraphTypeAttribute(Type type = null, bool exclude = false)
        {
            Type = type;
            Exclude = exclude;
        }
    }
}
