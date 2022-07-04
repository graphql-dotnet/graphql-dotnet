using GraphQL.Types;

namespace GraphQL
{
    /// <summary>
    /// Specifies an output graph type mapping for the CLR class or property marked with this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field)]
    public class OutputTypeAttribute : GraphQLAttribute
    {
        private Type _outputType = null!;

        /// <inheritdoc cref="OutputTypeAttribute"/>
        public OutputTypeAttribute(Type graphType)
        {
            OutputType = graphType;
        }

        /// <inheritdoc cref="OutputTypeAttribute"/>
        public Type OutputType
        {
            get => _outputType;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (!value.IsOutputType())
                    throw new ArgumentException(nameof(OutputType), $"'{value}' should be an output type");

                _outputType = value;
            }
        }

        /// <inheritdoc/>
        public override void Modify(FieldType fieldType, bool isInputType)
        {
            if (!isInputType)
            {
                fieldType.Type = _outputType;
            }
        }
    }
}
