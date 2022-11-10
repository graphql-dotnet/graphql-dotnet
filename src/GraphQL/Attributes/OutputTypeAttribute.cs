using GraphQL.Types;

namespace GraphQL
{
    /// <summary>
    /// Specifies an output graph type mapping for the CLR class or property marked with this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field)]
    public class OutputTypeAttribute<TGraphType> : GraphQLAttribute
        where TGraphType : IGraphType
    {
        /// <inheritdoc cref="OutputTypeAttribute{TGraphType}"/>
        public OutputTypeAttribute()
        {
            if (!typeof(TGraphType).IsOutputType())
                throw new ArgumentException($"'{typeof(TGraphType)}' should be an output type");
        }

        /// <inheritdoc/>
        public override void Modify(FieldType fieldType, bool isInputType)
        {
            if (!isInputType)
            {
                fieldType.Type = typeof(TGraphType);
            }
        }
    }
}
