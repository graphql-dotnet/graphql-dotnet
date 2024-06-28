using GraphQL.Types;
using static GraphQL.Federation.FederationHelper;

namespace GraphQL.Federation;

/// <inheritdoc cref="FederationMetadataExtensions"/>
public static class FederationInterfaceMetadataExtensions
{
    /// <summary>
    /// Adds the "@key" directive to a GraphQL type.
    /// <para>
    /// Designates an object type as an entity and specifies its key fields. Key fields are a set of fields
    /// that a subgraph can use to uniquely identify any instance of the entity.
    /// </para>
    /// </summary>
    /// <param name="graphType">The GraphQL type to which the directive is added.</param>
    /// <param name="fields">An array of field names that form the key.</param>
    /// <param name="resolvable">Indicates whether the key is resolvable. Default is true.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    /// <remarks>
    /// See <see href="https://www.apollographql.com/docs/federation/federated-types/federated-directives#key"/>.
    /// </remarks>
    public static TMetadataWriter Key<TMetadataWriter>(this TMetadataWriter graphType, string[] fields, bool resolvable = true)
        where TMetadataWriter : IMetadataWriter, IInterfaceGraphType
        => graphType.Key(string.Join(" ", fields), resolvable);

    /// <summary>
    /// Adds the "@key" directive to a GraphQL type.
    /// <para>
    /// Designates an object type as an entity and specifies its key fields. Key fields are a set of fields
    /// that a subgraph can use to uniquely identify any instance of the entity.
    /// </para>
    /// </summary>
    /// <param name="graphType">The GraphQL type to which the directive is added.</param>
    /// <param name="fields">A space-separated string of field names that form the key.</param>
    /// <param name="resolvable">Indicates whether the key is resolvable. Default is true.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    /// <remarks>
    /// See <see href="https://www.apollographql.com/docs/federation/federated-types/federated-directives#key"/>.
    /// </remarks>
    public static TMetadataWriter Key<TMetadataWriter>(this TMetadataWriter graphType, string fields, bool resolvable = true)
        where TMetadataWriter : IMetadataWriter, IInterfaceGraphType
        => graphType.ApplyDirective(KEY_DIRECTIVE, d =>
        {
            d.FromSchemaUrl = FEDERATION_LINK_SCHEMA_URL;
            d.AddArgument(new(FIELDS_ARGUMENT) { Value = fields });
            if (!resolvable)
                d.AddArgument(new(RESOLVABLE_ARGUMENT) { Value = false });
        });

}
