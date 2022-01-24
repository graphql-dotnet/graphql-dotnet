using System;
using GraphQL.Types;

namespace GraphQL
{
    /// <summary>
    /// Marks a class (graph type) or property (field) with additional metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class MetadataAttribute : GraphQLAttribute
    {
        /// <inheritdoc cref="MetadataAttribute"/>
        public MetadataAttribute(string key, object value)
        {
            Key = key;
            Value = value;
        }

        /// <summary>
        /// Gets or sets the metadata key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the metadata value.
        /// </summary>
        public object Value { get; set; }

        /// <inheritdoc/>
        public override void Modify(IGraphType graphType)
        {
            graphType.WithMetadata(Key, Value);
        }

        /// <inheritdoc/>
        public override void Modify(FieldType fieldType, bool isInputType)
        {
            fieldType.WithMetadata(Key, Value);
        }
    }
}
