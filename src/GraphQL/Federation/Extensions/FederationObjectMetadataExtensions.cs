using GraphQL.Types;
using static GraphQL.Federation.FederationHelper;

namespace GraphQL.Federation;

/// <inheritdoc cref="FederationMetadataExtensions"/>
public static class FederationObjectMetadataExtensions
{
    /// <inheritdoc cref="FederationInterfaceMetadataExtensions.Key{TMetadataWriter}(TMetadataWriter, string[], bool)"/>
    public static TMetadataWriter Key<TMetadataWriter>(this TMetadataWriter graphType, string[] fields, bool resolvable = true)
        where TMetadataWriter : IMetadataWriter, IObjectGraphType
        => graphType.Key(string.Join(" ", fields), resolvable);

    /// <inheritdoc cref="FederationInterfaceMetadataExtensions.Key{TMetadataWriter}(TMetadataWriter, string, bool)"/>
    public static TMetadataWriter Key<TMetadataWriter>(this TMetadataWriter graphType, string fields, bool resolvable = true)
        where TMetadataWriter : IMetadataWriter, IObjectGraphType
        => graphType.ApplyDirective(KEY_DIRECTIVE, d =>
        {
            d.AddArgument(new(FIELDS_ARGUMENT) { Value = fields });
            if (!resolvable)
                d.AddArgument(new(RESOLVABLE_ARGUMENT) { Value = false });
        });

    /// <summary>
    /// Adds the "@shareable" directive to a GraphQL type.
    /// <para>
    /// Indicates that an object type's field is allowed to be resolved by multiple subgraphs (by default in
    /// Federation 2, object fields can be resolved by only one subgraph).
    /// </para>
    /// </summary>
    /// <param name="graphType">The GraphQL type to which the directive is added.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    /// <remarks>
    /// See <see href="https://www.apollographql.com/docs/federation/federated-types/federated-directives#shareable"/>.
    /// </remarks>
    public static TMetadataWriter Shareable<TMetadataWriter>(this TMetadataWriter graphType)
        where TMetadataWriter : IMetadataWriter, IObjectGraphType
        => graphType.ApplyDirective(SHAREABLE_DIRECTIVE);

    /// <summary>
    /// Adds the "@external" directive to a GraphQL type.
    /// <para>
    /// Indicates that this subgraph usually can't resolve a particular object field, but it still needs
    /// to define that field for other purposes.
    /// </para>
    /// </summary>
    /// <param name="fieldType">The GraphQL type to which the directive is added.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    /// <remarks>
    /// See <see href="https://www.apollographql.com/docs/federation/federated-types/federated-directives#external"/>.
    /// </remarks>
    public static TMetadataWriter External<TMetadataWriter>(this TMetadataWriter fieldType)
        where TMetadataWriter : IMetadataWriter, IObjectGraphType
        => fieldType.ApplyDirective(EXTERNAL_DIRECTIVE);
}
