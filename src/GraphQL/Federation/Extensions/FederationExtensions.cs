using GraphQL.Types;
using static GraphQL.Federation.Extensions.FederationHelper;

namespace GraphQL.Federation.Extensions;

/// <summary>
/// Extension methods to configure federation directives within GraphQL schemas.
/// </summary>
public static class FederationExtensions
{
    /// <summary>
    /// Adds "@key" directive.
    /// </summary>
    public static TMetadataWriter Key<TMetadataWriter>(this TMetadataWriter graphType, string[] fields, bool resolvable = true)
        where TMetadataWriter : IMetadataWriter
        => graphType.Key(string.Join(" ", fields), resolvable);

    /// <summary>
    /// Adds "@key" directive.
    /// </summary>
    public static TMetadataWriter Key<TMetadataWriter>(this TMetadataWriter graphType, string fields, bool resolvable = true)
        where TMetadataWriter : IMetadataWriter
        => graphType.ApplyDirective(KEY_DIRECTIVE, d =>
        {
            d.AddArgument(new(FIELDS_ARGUMENT) { Value = fields });
            if (!resolvable)
                d.AddArgument(new(RESOLVABLE_ARGUMENT) { Value = false });
        });

    /// <summary>
    /// Adds "@shareable" directive.
    /// </summary>
    public static TMetadataWriter Shareable<TMetadataWriter>(this TMetadataWriter graphType)
        where TMetadataWriter : IMetadataWriter
        => graphType.ApplyDirective(SHAREABLE_DIRECTIVE);

    /// <summary>
    /// Adds "@inaccessible" directive.
    /// </summary>
    public static TMetadataWriter Inaccessible<TMetadataWriter>(this TMetadataWriter graphType)
        where TMetadataWriter : IMetadataWriter
        => graphType.ApplyDirective(INACCESSIBLE_DIRECTIVE);

    /// <summary>
    /// Adds "@override" directive.
    /// </summary>
    public static TMetadataWriter Override<TMetadataWriter>(this TMetadataWriter fieldType, string from)
        where TMetadataWriter : IMetadataWriter
        => fieldType.ApplyDirective(OVERRIDE_DIRECTIVE, d => d.AddArgument(new(FROM_ARGUMENT) { Value = from }));

    /// <summary>
    /// Adds "@external" directive.
    /// </summary>
    public static TMetadataWriter External<TMetadataWriter>(this TMetadataWriter fieldType)
        where TMetadataWriter : IMetadataWriter
        => fieldType.ApplyDirective(EXTERNAL_DIRECTIVE);

    /// <summary>
    /// Adds "@provides" directive.
    /// </summary>
    public static TMetadataWriter Provides<TMetadataWriter>(this TMetadataWriter fieldType, string[] fields)
        where TMetadataWriter : IMetadataWriter
        => fieldType.Provides(string.Join(" ", fields));

    /// <summary>
    /// Adds "@provides" directive.
    /// </summary>
    public static TMetadataWriter Provides<TMetadataWriter>(this TMetadataWriter fieldType, string fields)
        where TMetadataWriter : IMetadataWriter
        => fieldType.ApplyDirective(PROVIDES_DIRECTIVE, d => d.AddArgument(new(FIELDS_ARGUMENT) { Value = fields }));

    /// <summary>
    /// Adds "@requires" directive.
    /// </summary>
    public static TMetadataWriter Requires<TMetadataWriter>(this TMetadataWriter fieldType, string[] fields)
        where TMetadataWriter : IMetadataWriter
        => fieldType.Requires(string.Join(" ", fields));

    /// <summary>
    /// Adds "@requires" directive.
    /// </summary>
    public static TMetadataWriter Requires<TMetadataWriter>(this TMetadataWriter fieldType, string fields)
        where TMetadataWriter : IMetadataWriter
        => fieldType.ApplyDirective(REQUIRES_DIRECTIVE, d => d.AddArgument(new(FIELDS_ARGUMENT) { Value = fields }));
}
