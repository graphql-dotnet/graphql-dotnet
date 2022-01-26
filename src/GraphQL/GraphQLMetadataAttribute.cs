using System;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL
{
    /// <summary>
    /// Attribute for specifying additional information when matching a CLR type to a corresponding GraphType.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class GraphQLMetadataAttribute : GraphQLAttribute
    {
        private Type? _mappedToInput;
        private Type? _mappedToOutput;

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
        public string? Name { get; set; }

        /// <summary>
        /// GraphType description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Deprecation reason of the field or GraphType.
        /// </summary>
        public string? DeprecationReason { get; set; }

        public ResolverType ResolverType { get; set; }

        public Type? IsTypeOf { get; set; }

        /// <summary>
        /// Indicates which GraphType input type this CLR type is mapped to (if used in input context).
        /// </summary>
        [Obsolete("Please use the [InputType] attribute instead of this property.")]
        public Type? InputType
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
        [Obsolete("Please use the [OutputType] attribute instead of this property.")]
        public Type? OutputType
        {
            get => _mappedToOutput;
            set
            {
                if (value != null && !value.IsOutputType())
                    throw new ArgumentException(nameof(OutputType), $"'{value}' should be of output type");

                _mappedToOutput = value;
            }
        }

        /// <inheritdoc/>
        public override void Modify(TypeConfig type)
        {
            type.Description = Description;
            type.DeprecationReason = DeprecationReason;

            if (IsTypeOf != null)
                type.IsTypeOfFunc = t => IsTypeOf.IsAssignableFrom(t.GetType());
        }

        /// <inheritdoc/>
        public override void Modify(FieldConfig field)
        {
            field.Description = Description;
            field.DeprecationReason = DeprecationReason;
        }

        /// <inheritdoc/>
        public override void Modify(IGraphType graphType)
        {
            if (Name != null)
            {
                graphType.Name = Name;
            }

            if (Description != null)
                graphType.Description = Description == "" ? null : Description;

            if (DeprecationReason != null)
                graphType.DeprecationReason = DeprecationReason == "" ? null : DeprecationReason;
        }

        /// <inheritdoc/>
        public override void Modify(FieldType fieldType, bool isInputType)
        {
            if (Name != null)
            {
                fieldType.Name = Name;
            }

            if (Description != null)
                fieldType.Description = Description == "" ? null : Description;

            if (DeprecationReason != null)
                fieldType.DeprecationReason = DeprecationReason == "" ? null : DeprecationReason;

            if (isInputType && _mappedToInput != null)
                fieldType.Type = _mappedToInput;

            if (!isInputType && _mappedToOutput != null)
                fieldType.Type = _mappedToOutput;
        }
    }

    public enum ResolverType
    {
        Resolver,
        Subscriber
    }
}
