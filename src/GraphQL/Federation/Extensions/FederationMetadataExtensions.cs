using GraphQL.Types;
using static GraphQL.Federation.FederationHelper;

namespace GraphQL.Federation;

/// <summary>
/// Extension methods to configure federation directives within GraphQL schemas.
/// These methods allow the application of various federation directives such as @key, @shareable, @inaccessible, @override, @provides, and @requires to GraphQL types and fields.
/// </summary>
public static class FederationMetadataExtensions
{
    /// <summary>
    /// Adds the "@inaccessible" directive to a GraphQL type or field.
    /// <para>
    /// Indicates that a definition in the subgraph schema should be omitted from the router's API schema, even if that
    /// definition is also present in other subgraphs. This means that the field is not exposed to clients at all.
    /// </para>
    /// </summary>
    /// <param name="graphType">The GraphQL type or field to which the directive is added.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    /// <remarks>
    /// See <see href="https://www.apollographql.com/docs/federation/federated-types/federated-directives#inaccessible"/>.
    /// </remarks>
    public static TMetadataWriter Inaccessible<TMetadataWriter>(this TMetadataWriter graphType)
        where TMetadataWriter : IMetadataWriter
        => graphType.ApplyDirective(INACCESSIBLE_DIRECTIVE);
}
