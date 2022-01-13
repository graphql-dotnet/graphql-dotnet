namespace GraphQL.Transport
{
    /// <summary>
    /// Represents a message typically used by the graphql-ws or graphql-transport-ws WebSockets-based protocols.
    /// </summary>
    public class OperationMessage
    {
        /// <summary>
        /// Nullable Id
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Type of operation
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Nullable payload
        /// </summary>
        public object? Payload { get; set; }
    }
}
