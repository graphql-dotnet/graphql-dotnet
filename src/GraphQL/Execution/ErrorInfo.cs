namespace GraphQL.Execution
{
    /// <summary>
    /// Represents the fields of a GraphQL error entry. See https://spec.graphql.org/October2021/#sec-Errors
    /// </summary>
    public struct ErrorInfo
    {
        // The following descriptions are copied from https://spec.graphql.org/October2021/#sec-Errors

        /// <summary>
        /// A description of the error intended for the developer as a guide to understand and correct the error
        /// </summary>
        public string Message;

        /// <summary>
        /// This entry, if set, must have a map as its value. This entry is reserved for implementors to add additional
        /// information to errors however they see fit, and there are no additional restrictions on its contents.
        /// </summary>
        public IDictionary<string, object?>? Extensions;
    }
}
