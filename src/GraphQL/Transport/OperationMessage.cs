namespace GraphQL.Transport
{
    /// <summary>
    /// Represents a message typically used by the graphql-ws or graphql-transport-ws WebSockets-based protocols.
    /// </summary>
    public class OperationMessage
    {
        public const string ID_KEY = "id";
        public const string TYPE_KEY = "type";
        public const string PAYLOAD_KEY = "payload";

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
