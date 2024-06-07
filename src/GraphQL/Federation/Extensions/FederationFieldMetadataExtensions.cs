using GraphQL.Types;
using static GraphQL.Federation.FederationHelper;

namespace GraphQL.Federation;

/// <summary>
/// Extension methods to configure federation directives within GraphQL schemas.
/// These methods allow the application of various federation directives such as @key, @shareable, @inaccessible, @override, @external, @provides, and @requires to GraphQL types and fields.
/// </summary>
public static class FederationFieldMetadataExtensions
{
    /// <summary>
    /// Adds the "@shareable" directive to a GraphQL field.
    /// </summary>
    /// <param name="graphType">The GraphQL field to which the directive is added.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    public static TMetadataWriter Shareable<TMetadataWriter>(this TMetadataWriter graphType)
        where TMetadataWriter : IFieldMetadataWriter
        => graphType.ApplyDirective(SHAREABLE_DIRECTIVE);

    /// <summary>
    /// Adds the "@override" directive to a GraphQL field.
    /// </summary>
    /// <param name="fieldType">The GraphQL field to which the directive is added.</param>
    /// <param name="from">The name of the service from which the field is overridden.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    public static TMetadataWriter Override<TMetadataWriter>(this TMetadataWriter fieldType, string from)
        where TMetadataWriter : IFieldMetadataWriter
        => fieldType.ApplyDirective(OVERRIDE_DIRECTIVE, d => d.AddArgument(new(FROM_ARGUMENT) { Value = from }));

    /// <summary>
    /// Adds the "@external" directive to a GraphQL field.
    /// </summary>
    /// <param name="fieldType">The GraphQL field to which the directive is added.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    public static TMetadataWriter External<TMetadataWriter>(this TMetadataWriter fieldType)
        where TMetadataWriter : IFieldMetadataWriter
        => fieldType.ApplyDirective(EXTERNAL_DIRECTIVE);

    /// <summary>
    /// Adds the "@provides" directive to a GraphQL field.
    /// </summary>
    /// <param name="fieldType">The GraphQL field to which the directive is added.</param>
    /// <param name="fields">An array of field names that are provided by this field.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    public static TMetadataWriter Provides<TMetadataWriter>(this TMetadataWriter fieldType, string[] fields)
        where TMetadataWriter : IFieldMetadataWriter
        => fieldType.Provides(string.Join(" ", fields));

    /// <summary>
    /// Adds the "@provides" directive to a GraphQL field.
    /// </summary>
    /// <param name="fieldType">The GraphQL field to which the directive is added.</param>
    /// <param name="fields">A space-separated string of field names that are provided by this field.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    public static TMetadataWriter Provides<TMetadataWriter>(this TMetadataWriter fieldType, string fields)
        where TMetadataWriter : IFieldMetadataWriter
        => fieldType.ApplyDirective(PROVIDES_DIRECTIVE, d => d.AddArgument(new(FIELDS_ARGUMENT) { Value = fields }));

    /// <summary>
    /// Adds the "@requires" directive to a GraphQL field.
    /// </summary>
    /// <param name="fieldType">The GraphQL field to which the directive is added.</param>
    /// <param name="fields">An array of field names that are required by this field.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    public static TMetadataWriter Requires<TMetadataWriter>(this TMetadataWriter fieldType, string[] fields)
        where TMetadataWriter : IFieldMetadataWriter
        => fieldType.Requires(string.Join(" ", fields));

    /// <summary>
    /// Adds the "@requires" directive to a GraphQL field.
    /// </summary>
    /// <param name="fieldType">The GraphQL field to which the directive is added.</param>
    /// <param name="fields">A space-separated string of field names that are required by this field.</param>
    /// <typeparam name="TMetadataWriter">The type of the metadata writer.</typeparam>
    /// <returns>The modified metadata writer.</returns>
    public static TMetadataWriter Requires<TMetadataWriter>(this TMetadataWriter fieldType, string fields)
        where TMetadataWriter : IFieldMetadataWriter
        => fieldType.ApplyDirective(REQUIRES_DIRECTIVE, d => d.AddArgument(new(FIELDS_ARGUMENT) { Value = fields }));
}
