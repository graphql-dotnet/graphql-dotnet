using System;

namespace GraphQl.SchemaGenerator.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field)]
    public class GraphTypeAttribute : Attribute
    {
        public Type Type { get; set; }

        public GraphTypeAttribute()
        {

        }

        public GraphTypeAttribute(Type type)
        {
            Type = type;
        }
    }
}
