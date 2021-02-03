namespace GraphQL
{
    /// <summary>
    /// Options for configuring experimental features.
    /// </summary>
    public class ExperimentalFeatures
    {
        /// <summary>
        /// Enables ability to expose user-defined meta-information via introspection.
        /// See https://github.com/graphql/graphql-spec/issues/300 for more information.
        /// It is experimental feature that are not in the official specification (yet).
        /// </summary>
        public bool AppliedDirectives { get; set; } = false;

        /// <summary>
        /// Enables ability to expose 'isRepeatable' field for directives via introspection.
        /// This feature is from a working draft of the specification.
        /// </summary>
        public bool RepeatableDirectives { get; set; } = false;
    }
}
