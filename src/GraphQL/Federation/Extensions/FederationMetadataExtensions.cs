using GraphQL.Types;
using static GraphQL.Federation.FederationHelper;

namespace GraphQL.Federation;

/// <summary>
/// Extension methods to configure federation directives within GraphQL schemas.
/// These methods allow the application of various federation directives such as @key, @shareable, @inaccessible, @override, @external, @provides, and @requires to GraphQL types and fields.
/// </summary>
public static class FederationMetadataExtensions
{
    /// <summary>
    /// Adds the "@key" directive to a GraphQL type.
    /// </summary>
    /// <param name="graphType">The GraphQL type to which the directive is added.</param>
    /// <param name="fields">An array of field names that form the key.</param>
    /// <param name="resolvable">Indicates whether the key is resolvable. Default is true.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    public static TMetadataWriter Key<TMetadataWriter>(this TMetadataWriter graphType, string[] fields, bool resolvable = true)
        where TMetadataWriter : IMetadataWriter, IComplexGraphType
        => graphType.Key(string.Join(" ", fields), resolvable);

    /// <summary>
    /// Adds the "@key" directive to a GraphQL type.
    /// </summary>
    /// <param name="graphType">The GraphQL type to which the directive is added.</param>
    /// <param name="fields">A space-separated string of field names that form the key.</param>
    /// <param name="resolvable">Indicates whether the key is resolvable. Default is true.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    public static TMetadataWriter Key<TMetadataWriter>(this TMetadataWriter graphType, string fields, bool resolvable = true)
        where TMetadataWriter : IMetadataWriter, IComplexGraphType
        => graphType.ApplyDirective(KEY_DIRECTIVE, d =>
        {
            d.AddArgument(new(FIELDS_ARGUMENT) { Value = fields });
            if (!resolvable)
                d.AddArgument(new(RESOLVABLE_ARGUMENT) { Value = false });
        });

    /// <summary>
    /// Adds the "@shareable" directive to a GraphQL type.
    /// </summary>
    /// <param name="graphType">The GraphQL type to which the directive is added.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    public static TMetadataWriter Shareable<TMetadataWriter>(this TMetadataWriter graphType)
        where TMetadataWriter : IMetadataWriter, IObjectGraphType
        => graphType.ApplyDirective(SHAREABLE_DIRECTIVE);

    /// <summary>
    /// Adds the "@inaccessible" directive to a GraphQL type or field.
    /// </summary>
    /// <param name="graphType">The GraphQL type or field to which the directive is added.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    public static TMetadataWriter Inaccessible<TMetadataWriter>(this TMetadataWriter graphType)
        where TMetadataWriter : IMetadataWriter
        => graphType.ApplyDirective(INACCESSIBLE_DIRECTIVE);

    /// <summary>
    /// Adds the "@external" directive to a GraphQL type.
    /// </summary>
    /// <param name="fieldType">The GraphQL type to which the directive is added.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    public static TMetadataWriter External<TMetadataWriter>(this TMetadataWriter fieldType)
        where TMetadataWriter : IMetadataWriter, IObjectGraphType
        => fieldType.ApplyDirective(EXTERNAL_DIRECTIVE);
}
