using System;

namespace GraphQL
{
    /// <summary>
    /// Specifies an output graph type mapping for the CLR class marked with this attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class OutputTypeAttribute : Attribute
    {
        private Type _mappedToOutput = null!;

        /// <inheritdoc cref="OutputTypeAttribute"/>
        public OutputTypeAttribute(Type graphType)
        {
            OutputType = graphType;
        }

        /// <inheritdoc cref="OutputTypeAttribute"/>
        public Type OutputType
        {
            get => _mappedToOutput;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (!value.IsOutputType())
                    throw new ArgumentException(nameof(OutputType), $"'{value}' should be an output type");

                _mappedToOutput = value;
            }
        }
    }
}
