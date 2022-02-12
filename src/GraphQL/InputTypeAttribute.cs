using System;

namespace GraphQL
{
    /// <summary>
    /// Specifies an input graph type mapping for the CLR class marked with this attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class InputTypeAttribute : Attribute
    {
        private Type _mappedToInput = null!;

        /// <inheritdoc cref="InputTypeAttribute"/>
        public InputTypeAttribute(Type graphType)
        {
            InputType = graphType;
        }

        /// <inheritdoc cref="InputTypeAttribute"/>
        public Type InputType
        {
            get => _mappedToInput;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (!value.IsInputType())
                    throw new ArgumentException(nameof(InputType), $"'{value}' should be an input type");

                _mappedToInput = value;
            }
        }
    }
}
