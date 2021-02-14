using System;
using GraphQL.Utilities;

namespace GraphQL
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public abstract class GraphQLAttribute : Attribute
    {
        public virtual void Modify(TypeConfig type)
        {
        }

        public virtual void Modify(FieldConfig field)
        {
        }
    }

    /// <summary>
    /// Attribute for specifying additional information when matching a CLR type to a corresponding GraphType.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class GraphQLMetadataAttribute : GraphQLAttribute
    {
        private Type _mappedToInput;
        private Type _mappedToOutput;

        public GraphQLMetadataAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified GraphType name.
        /// </summary>
        /// <param name="name"></param>
        public GraphQLMetadataAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// GraphType name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// GraphType description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Deprecation reason of the field or GraphType.
        /// </summary>
        public string DeprecationReason { get; set; }

        public ResolverType ResolverType { get; set; }

        public Type IsTypeOf { get; set; }

        /// <summary>
        /// Indicates which GraphType input type this CLR type is mapped to (if used in input context).
        /// </summary>
        public Type InputType
        {
            get => _mappedToInput;
            set
            {
                if (value != null && !value.IsInputType())
                    throw new ArgumentException(nameof(InputType), $"'{value}' should be of input type");

                _mappedToInput = value;
            }
        }

        /// <summary>
        /// Indicates which GraphType output type this CLR type is mapped to (if used in output context).
        /// </summary>
        public Type OutputType
        {
            get => _mappedToOutput;
            set
            {
                if (value != null && !value.IsOutputType())
                    throw new ArgumentException(nameof(OutputType), $"'{value}' should be of output type");

                _mappedToOutput = value;
            }
        }

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
