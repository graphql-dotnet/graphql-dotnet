using System.Globalization;
using GraphQL.Federation.Types;
using GraphQL.Types;
using GraphQL.Utilities;
using GraphQLParser.AST;

namespace GraphQL.Federation;

/// <summary>
/// Provides helper methods for adding Federation directives to a GraphQL schema.
/// </summary>
internal static class FederationHelper
{
    public const string AST_METAFIELD = "__AST_MetaField__";
    public const string RESOLVER_METADATA = "__FedResolver__";
    public const string FEDERATION_RESOLVER_FIELD = "_FederationResolverField_";
    public const string LINK_SCHEMA_EXTENSION_METADATA = "__FedLinkSchemaExtension__";
    public const string FEDERATION_LINK_SCHEMA_URL = "https://specs.apollo.dev/federation/";

    public const string FEDERATION_LINK_PREFIX = "https://specs.apollo.dev/federation/v";

    public const string KEY_DIRECTIVE = "key";
    public const string SHAREABLE_DIRECTIVE = "shareable";
    public const string INACCESSIBLE_DIRECTIVE = "inaccessible";
    public const string OVERRIDE_DIRECTIVE = "override";
    public const string EXTERNAL_DIRECTIVE = "external";
    public const string PROVIDES_DIRECTIVE = "provides";
    public const string REQUIRES_DIRECTIVE = "requires";
    public const string FIELDS_ARGUMENT = "fields";
    public const string FROM_ARGUMENT = "from";
    public const string RESOLVABLE_ARGUMENT = "resolvable";
    public const string REPRESENTATIONS_ARGUMENT = "representations";
    public const string LINK_DIRECTIVE = "link";

    /// <summary>
    /// Links a specified version of the Federation schema to the specified
    /// schema with the @link directive.
    /// </summary>
    public static void AddFederationLink(this ISchema schema, string version, Action<LinkConfiguration>? configureLinkDirective = null)
    {
        if (version.StartsWith("1."))
            throw new ArgumentOutOfRangeException(nameof(version), version, "The @link directive is only supported by Apollo Federation v2 and newer.");

        // configure all directives available in Federation v2.0, which
        // are the most commonly-used directives; other directives and types
        // will be imported into the 'federation' namespace (e.g. 'federation__FieldSet')
        Action<LinkConfiguration> configure = c =>
        {
            c.Imports.Add("@key", "@key");
            c.Imports.Add("@external", "@external");
            c.Imports.Add("@requires", "@requires");
            c.Imports.Add("@provides", "@provides");
            c.Imports.Add("@shareable", "@shareable");
            c.Imports.Add("@inaccessible", "@inaccessible");
            c.Imports.Add("@override", "@override");
            c.Imports.Add("@tag", "@tag");
        };

        // include any custom configuration
        configure += configureLinkDirective;

        // add the @link directive to the schema
        schema.LinkSchema(FEDERATION_LINK_PREFIX + version, configure);
    }

    /// <summary>
    /// Adds Federation type and directive definitions to the specified GraphQL schema based on
    /// the version number and/or `@link` directive.
    /// </summary>
    /// <param name="schema">The GraphQL schema to add directives to.</param>
    /// <param name="versionString">The version of the Federation specification to use.</param>
    public static void AddFederationTypesAndDirectives(this ISchema schema, string versionString)
    {
        if (!TryParseVersion(versionString, out var version))
            throw new InvalidOperationException($"Cannot parse Federation version number '{versionString}'.");
        LinkConfiguration link;
        if (versionString.StartsWith("1."))
        {
            link = new(FEDERATION_LINK_PREFIX + versionString);
            link.Imports.Add("FieldSet", "FieldSet");
            link.Imports.Add("@key", "@key");
            link.Imports.Add("@extends", "@extends");
            link.Imports.Add("@external", "@external");
            link.Imports.Add("@requires", "@requires");
            link.Imports.Add("@provides", "@provides");
            if (versionString == "1.1")
                link.Imports.Add("@tag", "@tag");
        }
        else
        {
            var links = schema.GetLinkedSchemas().Where(x => x.Url == FEDERATION_LINK_PREFIX + versionString).ToList();
            if (links.Count == 0)
                throw new InvalidOperationException("The schema must be linked to a Federation schema.");
            if (links.Count > 1)
                throw new InvalidOperationException("The schema must be linked to only one Federation schema.");
            link = links[0];
        }

        // FieldSet
        var fieldSetType = new FieldSetGraphType { Name = link.NameForType("FieldSet") };
        schema.RegisterType(fieldSetType);

        // @key
        RegisterDirective(
            KEY_DIRECTIVE, 1, 0,
            [
                new QueryArgument(new NonNullGraphType(fieldSetType)) { Name = FIELDS_ARGUMENT },
                new QueryArgument<BooleanGraphType>() { Name = RESOLVABLE_ARGUMENT, DefaultValue = true }
            ],
            [DirectiveLocation.Object], true,
            c =>
            {
                if (IsMinimumVersion(2, 3))
                    c.Locations.Add(DirectiveLocation.Interface);
            });

        // @interfaceObject
        RegisterDirective(
            "interfaceObject", 2, 3,
            null,
            [DirectiveLocation.Object], false);

        // @extends
        RegisterDirective("extends", 1, 0, null, [DirectiveLocation.Object, DirectiveLocation.Interface], false);

        // @shareable
        RegisterDirective(SHAREABLE_DIRECTIVE, 2, 0, null, [DirectiveLocation.FieldDefinition, DirectiveLocation.Object], false, c =>
        {
            if (IsMinimumVersion(2, 2))
                c.Repeatable = true;
        });

        // @inaccessible
        RegisterDirective(INACCESSIBLE_DIRECTIVE, 2, 0, null,
        [
            DirectiveLocation.FieldDefinition,
            DirectiveLocation.Interface,
            DirectiveLocation.Object,
            DirectiveLocation.Union,
            DirectiveLocation.ArgumentDefinition,
            DirectiveLocation.Scalar,
            DirectiveLocation.Enum,
            DirectiveLocation.EnumValue,
            DirectiveLocation.InputObject,
            DirectiveLocation.InputFieldDefinition,
        ], false);

        // @override
        RegisterDirective(OVERRIDE_DIRECTIVE, 2, 0,
            [new QueryArgument<NonNullGraphType<StringGraphType>>() { Name = FROM_ARGUMENT }],
            [DirectiveLocation.FieldDefinition], false, c =>
            {
                if (IsMinimumVersion(2, 7))
                    c.Arguments!.Add(new QueryArgument<StringGraphType>() { Name = "label" });
            });

        // @authenticated
        RegisterDirective("authenticated", 2, 5, null, [
            DirectiveLocation.FieldDefinition,
            DirectiveLocation.Object,
            DirectiveLocation.Interface,
            DirectiveLocation.Scalar,
            DirectiveLocation.Enum,
        ], false);

        // @requiresScopes
        RegisterDirective("requiresScopes", 2, 5,
            [new QueryArgument<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>() { Name = "scopes" }],
            [
                DirectiveLocation.FieldDefinition,
                DirectiveLocation.Object,
                DirectiveLocation.Interface,
                DirectiveLocation.Scalar,
                DirectiveLocation.Enum,
            ], false);

        // @policy
        RegisterDirective("policy", 2, 6,
            [new QueryArgument<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>() { Name = "policies" }],
            [
                DirectiveLocation.FieldDefinition,
                DirectiveLocation.Object,
                DirectiveLocation.Interface,
                DirectiveLocation.Scalar,
                DirectiveLocation.Enum,
            ], false);

        // @external
        RegisterDirective(EXTERNAL_DIRECTIVE, 1, 0, null, [DirectiveLocation.FieldDefinition, DirectiveLocation.Object], false);

        // @provides
        RegisterDirective(PROVIDES_DIRECTIVE, 1, 0,
            [new QueryArgument(new NonNullGraphType(fieldSetType)) { Name = FIELDS_ARGUMENT }],
            [DirectiveLocation.FieldDefinition], false);

        // @requires
        RegisterDirective(REQUIRES_DIRECTIVE, 1, 0,
            [new QueryArgument(new NonNullGraphType(fieldSetType)) { Name = FIELDS_ARGUMENT }],
            [DirectiveLocation.FieldDefinition], false);

        // @tag
        RegisterDirective("tag", 1, 1, [new QueryArgument<NonNullGraphType<StringGraphType>>() { Name = "name" }], [
            DirectiveLocation.FieldDefinition,
            DirectiveLocation.Interface,
            DirectiveLocation.Object,
            DirectiveLocation.Union,
            DirectiveLocation.ArgumentDefinition,
            DirectiveLocation.Scalar,
            DirectiveLocation.Enum,
            DirectiveLocation.EnumValue,
            DirectiveLocation.InputObject,
            DirectiveLocation.InputFieldDefinition,
            DirectiveLocation.Schema,
        ], true);

        // @composeDirective
        RegisterDirective("composeDirective", 2, 1, [new QueryArgument<NonNullGraphType<StringGraphType>>() { Name = "name" }], [DirectiveLocation.Schema], true);

        // @context
        RegisterDirective("context", 2, 8, [new QueryArgument<NonNullGraphType<StringGraphType>>() { Name = "name" }], [
            DirectiveLocation.Object,
            DirectiveLocation.Interface,
            DirectiveLocation.Union,
        ], false);

        // ContextFieldValue
        var contextFieldValue = new ContextFieldValueGraphType { Name = link.NameForType("ContextFieldValue") };
        if (IsMinimumVersion(2, 8))
            schema.RegisterType(contextFieldValue);

        // @fromContext
        RegisterDirective("fromContext", 2, 8, [new QueryArgument(contextFieldValue) { Name = "field" }], [DirectiveLocation.ArgumentDefinition], false);


        bool IsMinimumVersion(int major, int minor)
            => version.Major > major || (version.Major == major && version.Minor >= minor);

        void RegisterDirective(string directiveName,
            int minMajor,
            int minMinor,
            QueryArgument[]? arguments,
            DirectiveLocation[] locations,
            bool repeatable,
            Action<Directive>? configuration = null)
        {
            if (!IsMinimumVersion(minMajor, minMinor))
                return;
            var directive = new Directive(link.NameForDirective(directiveName))
            {
                Arguments = arguments != null ? new QueryArguments(arguments) : null,
                Repeatable = repeatable,
            };
            foreach (var location in locations)
                directive.Locations.Add(location);
            configuration?.Invoke(directive);
            schema.Directives.Register(directive);
        }
    }

    /// <summary>
    /// Parses the specified version string into a tuple of major and minor version numbers.
    /// </summary>
    internal static bool TryParseVersion(string versionString, out (int Major, int Minor) version)
    {
        version = (0, 0);
        if (string.IsNullOrWhiteSpace(versionString))
            return false;

        string[] parts = versionString.Split('.');
        if (parts.Length != 2)
            return false;

        if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int major) ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int minor))
            return false;

        version = (major, minor);
        return true;
    }
}
