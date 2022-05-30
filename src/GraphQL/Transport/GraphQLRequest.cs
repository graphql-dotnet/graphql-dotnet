namespace GraphQL.Transport
{
    /// <summary>
    /// Represents data sent by client to GraphQL server.
    /// See https://github.com/graphql/graphql-over-http/blob/master/spec/GraphQLOverHTTP.md#request
    /// </summary>
    public class GraphQLRequest
    {
        /// <summary>
        /// The name of the Operation in the Document to execute (optional).
        /// </summary>
        public string? OperationName { get; set; }

        /// <summary>
        /// A Document containing GraphQL Operations and Fragments to execute.
        /// It can be null in case of automatic persisted queries (https://www.apollographql.com/docs/apollo-server/performance/apq/)
        /// when a client sends only SHA-256 hash of the query in <see cref="Extensions"/> given that corresponding key-value pair has been saved on a server beforehand.
        /// </summary>
        public string? Query { get; set; }

        /// <summary>
        /// Values for any Variables defined by the Operation (optional).
        /// </summary>
        public Inputs? Variables { get; set; }

        /// <summary>
        /// This entry is reserved for implementors to extend the protocol however they see fit (optional).
        /// </summary>
        public Inputs? Extensions { get; set; }
    }
}
