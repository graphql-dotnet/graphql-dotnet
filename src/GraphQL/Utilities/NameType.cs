namespace GraphQL.Utilities
{
    /// <summary>
    /// Indicates a field, type, argument, enum or directive.
    /// </summary>
    public enum NameType
    {
        /// <summary>
        /// A field
        /// </summary>
        Field,

        /// <summary>
        /// A type
        /// </summary>
        Type,

        /// <summary>
        /// An argument
        /// </summary>
        Argument,

        /// <summary>
        /// An enum
        /// </summary>
        Enum,

        /// <summary>
        /// A directive
        /// </summary>
        Directive,
    }
}
