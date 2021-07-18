namespace GraphQL.Execution
{
    /// <summary>
    /// Represents the value of an argument.
    /// </summary>
    public readonly struct ArgumentValue
    {
        /// <summary>
        /// Returns an instance of this struct containing a <see langword="null"/> value supplied as a literal.
        /// </summary>
        public static ArgumentValue NullLiteral => new ArgumentValue(null, ArgumentSource.Literal);

        /// <summary>
        /// Returns an instance of this struct containing a <see langword="null"/> value supplied as a variable.
        /// </summary>
        public static ArgumentValue NullVariable => new ArgumentValue(null, ArgumentSource.Variable);

        /// <summary>
        /// Initializes a new instance with the specified values.
        /// </summary>
        public ArgumentValue(object? value, ArgumentSource source)
        {
            Value = value;
            Source = source;
        }

        /// <summary>
        /// Returns the value of the argument.
        /// </summary>
        public object? Value { get; }

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

        /// <summary>
        /// A default value for a variable referenced by the argument.
        /// </summary>
        VariableDefault,
    }
}
