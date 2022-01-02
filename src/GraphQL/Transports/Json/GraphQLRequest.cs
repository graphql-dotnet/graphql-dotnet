namespace GraphQL.Transports.Json
{
    /// <summary>
    /// Represents data sent by client to GraphQL server.
    /// See https://github.com/graphql/graphql-over-http/blob/master/spec/GraphQLOverHTTP.md#request
    /// </summary>
    public class GraphQLRequest
    {
        /// <summary>
        /// Name for the operation name parameter.
        /// See https://github.com/graphql/graphql-over-http/blob/master/spec/GraphQLOverHTTP.md#request-parameters
        /// </summary>
        public const string OPERATION_NAME_KEY = "operationName";

        /// <summary>
        /// Name for the query parameter.
        /// See https://github.com/graphql/graphql-over-http/blob/master/spec/GraphQLOverHTTP.md#request-parameters
        /// </summary>
        public const string QUERY_KEY = "query";

        /// <summary>
        /// Name for the variables parameter.
        /// See https://github.com/graphql/graphql-over-http/blob/master/spec/GraphQLOverHTTP.md#request-parameters
        /// </summary>
        public const string VARIABLES_KEY = "variables";

        /// <summary>
        /// Name for the extensions parameter.
        /// See https://github.com/graphql/graphql-over-http/blob/master/spec/GraphQLOverHTTP.md#request-parameters
        /// </summary>
        public const string EXTENSIONS_KEY = "extensions";

        /// <summary>
        /// The name of the Operation in the Document to execute (optional).
        /// </summary>
        public string? OperationName { get; set; }

        /// <summary>
        /// A Document containing GraphQL Operations and Fragments to execute (required).
        /// </summary>
        public string Query { get; set; } = null!;

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
