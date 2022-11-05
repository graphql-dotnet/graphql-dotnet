using GraphQL.Types;

namespace GraphQL
{
    /// <summary>
    /// Specifies an input graph type mapping for the CLR class or property marked with this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class InputTypeAttribute : GraphQLAttribute
    {
        private Type _inputType = null!;

        /// <inheritdoc cref="InputTypeAttribute"/>
        public InputTypeAttribute(Type graphType)
        {
            InputType = graphType;
        }

        /// <inheritdoc cref="InputTypeAttribute"/>
        public Type InputType
        {
            get => _inputType;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (!value.IsInputType())
                    throw new ArgumentException(nameof(InputType), $"'{value}' should be an input type");

                _inputType = value;
            }
        }

        /// <inheritdoc/>
        public override void Modify(FieldType fieldType, bool isInputType)
        {
            if (isInputType)
            {
                fieldType.Type = _inputType;
            }
        }

        /// <inheritdoc/>
        public override void Modify(QueryArgument queryArgument)
        {
            queryArgument.Type = _inputType;
        }
    }
}
