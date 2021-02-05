namespace GraphQL.Utilities
{
    /// <summary>
    /// Indicates a field, type, argument, enum or directive.
    /// </summary>
    public enum NamedElement
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
        /// An enum value
        /// </summary>
        EnumValue,

        /// <summary>
        /// A directive
        /// </summary>
        Directive,
    }
}
