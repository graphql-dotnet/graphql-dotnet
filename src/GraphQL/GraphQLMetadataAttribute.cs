using System;
using GraphQL.Utilities;

namespace GraphQL
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
    public abstract class GraphQLAttribute : Attribute
    {
        public virtual void Modify(TypeConfig type)
        {
        }

        public virtual void Modify(FieldConfig field)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class GraphQLMetadataAttribute : GraphQLAttribute
    {
        public GraphQLMetadataAttribute()
        {
        }

        public GraphQLMetadataAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public string DeprecationReason { get; set; }
        public ResolverType Type { get; set; }

        public Type IsTypeOf { get; set; }

        public override void Modify(TypeConfig type)
        {
            type.Description = Description;
            type.DeprecationReason = DeprecationReason;

            if (IsTypeOf != null)
                type.IsTypeOfFunc = t => IsTypeOf.IsAssignableFrom(t.GetType());
        }

        public override void Modify(FieldConfig field)
        {
            field.Description = Description;
            field.DeprecationReason = DeprecationReason;
        }
    }

    public enum ResolverType
    {
        Resolver,
        Subscriber
    }
}
