namespace GraphQL.Transports.Json
{
    public class WebSocketMessage
    {
        public const string ID_KEY = "id";
        public const string TYPE_KEY = "type";
        public const string PAYLOAD_KEY = "payload";

        /// <summary>
        /// Nullable Id
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Type of packet
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Nullable payload
        /// </summary>
        public object? Payload { get; set; }
    }
}
