namespace GraphQL.Execution
{
    /// <summary>
    /// Represents the value of an argument.
    /// </summary>
    public struct ArgumentValue
    {
        /// <summary>
        /// Initializes a new instance with the specified values.
        /// </summary>
        public ArgumentValue(object value, ArgumentSource source)
        {
            Value = value;
            Source = source;
        }

        /// <summary>
        /// Returns the value of the argument.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Returns a value indicating the source of the argument's value.
        /// </summary>
        public ArgumentSource Source { get; }
    }

    /// <summary>
    /// The source of an argument's value.
    /// </summary>
    public enum ArgumentSource
    {
        /// <summary>
        /// The field default value.
        /// </summary>
        FieldDefault,

        /// <summary>
        /// A literal value supplied for the argument.
        /// </summary>
        Literal,

        /// <summary>
        /// A variable referenced by the argument.
        /// </summary>
        Variable,
    }

}
