namespace GraphQL.Utilities.Federation.Enums
{
    /// <summary>
    /// Enumeration of all Federation directives.
    /// </summary>
    [Flags]
    public enum FederationDirectiveEnum
    {
        /// <summary>
        /// No directives defined
        /// </summary>
        None = 0,
        /// <summary>
        /// @key directive.
        /// </summary>
        Key = 1,
        /// <summary>
        /// @shareable directive.
        /// </summary>
        Shareable = 2,
        /// <summary>
        /// @inaccessible directive.
        /// </summary>
        Inaccessible = 4,
        /// <summary>
        /// @override directive.
        /// </summary>
        Override = 8,
        /// <summary>
        /// @external directive.
        /// </summary>
        External = 16,
        /// <summary>
        /// @provides directive.
        /// </summary>
        Provides = 32,
        /// <summary>
        /// @requires directive.
        /// </summary>
        Requires = 64
    }
}
