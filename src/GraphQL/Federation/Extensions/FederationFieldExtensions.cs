using GraphQL.Builders;
using GraphQL.Types;
using GraphQLParser.AST;
using static GraphQL.Federation.Extensions.FederationHelper;

namespace GraphQL.Federation.Extensions;

/// <summary>
/// Federation extensions for Fields.
/// </summary>
public static class FederationFieldExtensions
{
    /// <summary>
    /// Adds "@shareable" directive.
    /// </summary>
    public static FieldBuilder<TSourceType, TReturnType> Shareable<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> fieldBuilder)
    { fieldBuilder.FieldType.Shareable(); return fieldBuilder; }

    /// <summary>
    /// Adds "@inaccessible" directive.
    /// </summary>
    public static FieldBuilder<TSourceType, TReturnType> Inaccessible<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> fieldBuilder)
    { fieldBuilder.FieldType.Inaccessible(); return fieldBuilder; }

    /// <summary>
    /// Adds "@override" directive.
    /// </summary>
    public static FieldBuilder<TSourceType, TReturnType> Override<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> fieldBuilder, string from)
    { fieldBuilder.FieldType.Override(from); return fieldBuilder; }

    /// <summary>
    /// Adds "@external" directive.
    /// </summary>
    public static FieldBuilder<TSourceType, TReturnType> External<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> fieldBuilder)
    { fieldBuilder.FieldType.External(); return fieldBuilder; }

    /// <summary>
    /// Adds "@provides" directive.
    /// </summary>
    public static FieldBuilder<TSourceType, TReturnType> Provides<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> fieldBuilder, string fields)
    { fieldBuilder.FieldType.Provides(fields); return fieldBuilder; }

    /// <summary>
    /// Adds "@requires" directive.
    /// </summary>
    public static FieldBuilder<TSourceType, TReturnType> Requires<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> fieldBuilder, string fields)
    { fieldBuilder.FieldType.Requires(fields); return fieldBuilder; }

    /// <summary>
    /// Adds "@shareable" directive.
    /// </summary>
    public static void Shareable(this FieldType fieldType)
    {
        var astMetadata = fieldType.BuildAstMetadata();
        var directive = new GraphQLDirective { Name = new(SHAREABLE_DIRECTIVE) };
        astMetadata!.Directives!.Items.Add(directive);
    }

    /// <summary>
    /// Adds "@inaccessible" directive.
    /// </summary>
    public static void Inaccessible(this FieldType fieldType)
    {
        var astMetadata = fieldType.BuildAstMetadata();
        var directive = new GraphQLDirective { Name = new(INACCESSIBLE_DIRECTIVE) };
        astMetadata!.Directives!.Items.Add(directive);
    }

    /// <summary>
    /// Adds "@override" directive.
    /// </summary>
    public static void Override(this FieldType fieldType, string from)
    {
        var astMetadata = fieldType.BuildAstMetadata();
        var directive = new GraphQLDirective { Name = new(OVERRIDE_DIRECTIVE) };
        directive.AddFromArgument(from);
        astMetadata!.Directives!.Items.Add(directive);
    }

    /// <summary>
    /// Adds "@external" directive.
    /// </summary>
    public static void External(this FieldType fieldType)
    {
        var astMetadata = fieldType.BuildAstMetadata();
        var directive = new GraphQLDirective { Name = new(EXTERNAL_DIRECTIVE) };
        astMetadata!.Directives!.Items.Add(directive);
    }

    /// <summary>
    /// Adds "@provides" directive.
    /// </summary>
    public static void Provides(this FieldType fieldType, string[] fields) =>
        fieldType.Provides(string.Join(" ", fields.Select(x => x.ToCamelCase())));

    /// <summary>
    /// Adds "@provides" directive.
    /// </summary>
    public static void Provides(this FieldType fieldType, string fields)
    {
        var astMetadata = fieldType.BuildAstMetadata();
        var directive = new GraphQLDirective { Name = new(PROVIDES_DIRECTIVE) };
        directive.AddFieldsArgument(fields.ToCamelCase());
        astMetadata!.Directives!.Items.Add(directive);
    }

    /// <summary>
    /// Adds "@requires" directive.
    /// </summary>
    public static void Requires(this FieldType fieldType, string[] fields) =>
        fieldType.Requires(string.Join(" ", fields.Select(x => x.ToCamelCase())));

    /// <summary>
    /// Adds "@requires" directive.
    /// </summary>
    public static void Requires(this FieldType fieldType, string fields)
    {
        var astMetadata = fieldType.BuildAstMetadata();
        var directive = new GraphQLDirective { Name = new(REQUIRES_DIRECTIVE) };
        directive.AddFieldsArgument(fields.ToCamelCase());
        astMetadata!.Directives!.Items.Add(directive);
    }
}
