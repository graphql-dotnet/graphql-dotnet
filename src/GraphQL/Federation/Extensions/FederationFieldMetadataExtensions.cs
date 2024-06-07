using GraphQL.Types;
using static GraphQL.Federation.FederationHelper;

namespace GraphQL.Federation;

/// <inheritdoc cref="FederationMetadataExtensions"/>
public static class FederationFieldMetadataExtensions
{
    /// <summary>
    /// Adds the "@shareable" directive to a GraphQL field.
    /// <para>
    /// Indicates that an object type's field is allowed to be resolved by multiple subgraphs (by default in
    /// Federation 2, object fields can be resolved by only one subgraph).
    /// </para>
    /// </summary>
    /// <param name="graphType">The GraphQL field to which the directive is added.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    /// <remarks>
    /// See <see href="https://www.apollographql.com/docs/federation/federated-types/federated-directives#shareable"/>.
    /// </remarks>
    public static TMetadataWriter Shareable<TMetadataWriter>(this TMetadataWriter graphType)
        where TMetadataWriter : IFieldMetadataWriter
        => graphType.ApplyDirective(SHAREABLE_DIRECTIVE);

    /// <summary>
    /// Adds the "@override" directive to a GraphQL field.
    /// <para>
    /// Indicates that an object field is now resolved by this subgraph instead of another subgraph where it's also
    /// defined. This enables you to migrate a field from one subgraph to another.
    /// </para>
    /// </summary>
    /// <param name="fieldType">The GraphQL field to which the directive is added.</param>
    /// <param name="from">The name of the service from which the field is overridden.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    /// <remarks>
    /// See <see href="https://www.apollographql.com/docs/federation/federated-types/federated-directives#override"/>.
    /// </remarks>
    public static TMetadataWriter Override<TMetadataWriter>(this TMetadataWriter fieldType, string from)
        where TMetadataWriter : IFieldMetadataWriter
        => fieldType.ApplyDirective(OVERRIDE_DIRECTIVE, d => d.AddArgument(new(FROM_ARGUMENT) { Value = from }));

    /// <summary>
    /// Adds the "@external" directive to a GraphQL field.
    /// <para>
    /// Indicates that this subgraph usually can't resolve a particular object field, but it still needs to
    /// define that field for other purposes.
    /// </para>
    /// </summary>
    /// <param name="fieldType">The GraphQL field to which the directive is added.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    /// <remarks>
    /// See <see href="https://www.apollographql.com/docs/federation/federated-types/federated-directives#external"/>.
    /// </remarks>
    public static TMetadataWriter External<TMetadataWriter>(this TMetadataWriter fieldType)
        where TMetadataWriter : IFieldMetadataWriter
        => fieldType.ApplyDirective(EXTERNAL_DIRECTIVE);

    /// <summary>
    /// Adds the "@provides" directive to a GraphQL field.
    /// <para>
    /// Specifies a set of entity fields that a subgraph can resolve, but only at a particular schema path (at other
    /// paths, the subgraph can't resolve those fields).
    /// </para>
    /// </summary>
    /// <param name="fieldType">The GraphQL field to which the directive is added.</param>
    /// <param name="fields">An array of field names that are provided by this field.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    /// <remarks>
    /// See <see href="https://www.apollographql.com/docs/federation/federated-types/federated-directives#provides"/>.
    /// </remarks>
    public static TMetadataWriter Provides<TMetadataWriter>(this TMetadataWriter fieldType, string[] fields)
        where TMetadataWriter : IFieldMetadataWriter
        => fieldType.Provides(string.Join(" ", fields));

    /// <summary>
    /// Adds the "@provides" directive to a GraphQL field.
    /// <para>
    /// Specifies a set of entity fields that a subgraph can resolve, but only at a particular schema path (at other
    /// paths, the subgraph can't resolve those fields).
    /// </para>
    /// </summary>
    /// <param name="fieldType">The GraphQL field to which the directive is added.</param>
    /// <param name="fields">A space-separated string of field names that are provided by this field.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    /// <remarks>
    /// See <see href="https://www.apollographql.com/docs/federation/federated-types/federated-directives#provides"/>.
    /// </remarks>
    public static TMetadataWriter Provides<TMetadataWriter>(this TMetadataWriter fieldType, string fields)
        where TMetadataWriter : IFieldMetadataWriter
        => fieldType.ApplyDirective(PROVIDES_DIRECTIVE, d => d.AddArgument(new(FIELDS_ARGUMENT) { Value = fields }));

    /// <summary>
    /// Adds the "@requires" directive to a GraphQL field.
    /// <para>
    /// Indicates that the resolver for a particular entity field depends on the values of other entity fields that
    /// are resolved by other subgraphs. This tells the router that it needs to fetch the values of those externally
    /// defined fields first, even if the original client query didn't request them.
    /// </para>
    /// </summary>
    /// <param name="fieldType">The GraphQL field to which the directive is added.</param>
    /// <param name="fields">An array of field names that are required by this field.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    /// <remarks>
    /// See <see href="https://www.apollographql.com/docs/federation/federated-types/federated-directives#requires"/>.
    /// </remarks>
    public static TMetadataWriter Requires<TMetadataWriter>(this TMetadataWriter fieldType, string[] fields)
        where TMetadataWriter : IFieldMetadataWriter
        => fieldType.Requires(string.Join(" ", fields));

    /// <summary>
    /// Adds the "@requires" directive to a GraphQL field.
    /// <para>
    /// Indicates that the resolver for a particular entity field depends on the values of other entity fields that
    /// are resolved by other subgraphs. This tells the router that it needs to fetch the values of those externally
    /// defined fields first, even if the original client query didn't request them.
    /// </para>
    /// </summary>
    /// <param name="fieldType">The GraphQL field to which the directive is added.</param>
    /// <param name="fields">A space-separated string of field names that are required by this field.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    /// <remarks>
    /// See <see href="https://www.apollographql.com/docs/federation/federated-types/federated-directives#requires"/>.
    /// </remarks>
    public static TMetadataWriter Requires<TMetadataWriter>(this TMetadataWriter fieldType, string fields)
        where TMetadataWriter : IFieldMetadataWriter
        => fieldType.ApplyDirective(REQUIRES_DIRECTIVE, d => d.AddArgument(new(FIELDS_ARGUMENT) { Value = fields }));
}
