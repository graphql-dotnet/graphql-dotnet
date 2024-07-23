namespace GraphQL.Transport;

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

    /// <summary>
    /// A unique identifier for the document (optional). When in use, the <see cref="Query">Query</see> property should
    /// not be provided. Typically requires the persisted document handler configured.
    /// <para>
    /// The identifier may be either a prefixed document identifier or a custom document identifier.
    /// </para>
    /// <para>
    /// A prefixed identifier must be a string in the format of "{prefix}:{payload}". For the prefix "sha256",
    /// the payload must be a lower-case hexadecimal encoding of the SHA256 hash of the query string. Applications
    /// may define their own prefixes which must start with "x-"; other prefixes are reserved for future use.
    /// </para>
    /// <para>
    /// Custom document identifiers are defined by the application and must not contain a colon.
    /// </para>
    /// </summary>
    public string? DocumentId { get; set; }
}
