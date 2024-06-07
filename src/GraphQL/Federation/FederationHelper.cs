//using GraphQL.Federation.Types;
//using GraphQL.Types;
//using GraphQLParser.AST;

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

    public const string LINK_DIRECTIVE = "link";
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
    public const string URL_ARGUMENT = "url";
    public const string AS_ARGUMENT = "as";
    public const string FOR_ARGUMENT = "for";
    public const string IMPORT_ARGUMENT = "import";

    /*
    /// <summary>
    /// Maps <see cref="FederationDirectiveEnum"/> values to their corresponding directive strings.
    /// </summary>
    private static readonly Dictionary<FederationDirectiveEnum, string> _federationDirectiveEnumMap = new()
    {
        [FederationDirectiveEnum.Key] = $"@{KEY_DIRECTIVE}",
        [FederationDirectiveEnum.Shareable] = $"@{SHAREABLE_DIRECTIVE}",
        [FederationDirectiveEnum.Inaccessible] = $"@{INACCESSIBLE_DIRECTIVE}",
        [FederationDirectiveEnum.Override] = $"@{OVERRIDE_DIRECTIVE}",
        [FederationDirectiveEnum.External] = $"@{EXTERNAL_DIRECTIVE}",
        [FederationDirectiveEnum.Provides] = $"@{PROVIDES_DIRECTIVE}",
        [FederationDirectiveEnum.Requires] = $"@{REQUIRES_DIRECTIVE}",
    };

    /// <summary>
    /// Adds Federation directive definitions to the specified GraphQL schema based on the provided <see cref="FederationDirectiveEnum"/> values.
    /// </summary>
    /// <param name="schema">The GraphQL schema to add directives to.</param>
    /// <param name="import">The <see cref="FederationDirectiveEnum"/> values specifying which directives to add.</param>
    public static void AddFederationDirectives(this ISchema schema, FederationDirectiveEnum import)
    {
        var linkDirective = new Directive(LINK_DIRECTIVE)
        {
            Arguments = new(
                new QueryArgument<NonNullGraphType<StringGraphType>>() { Name = URL_ARGUMENT },
                new QueryArgument<StringGraphType>() { Name = AS_ARGUMENT },
                new QueryArgument<LinkPurposeGraphType>() { Name = FOR_ARGUMENT },
                new QueryArgument<ListGraphType<LinkImportGraphType>>() { Name = IMPORT_ARGUMENT }
            ),
            Repeatable = true,
        };
        linkDirective.Locations.Add(DirectiveLocation.Schema);
        schema.Directives.Register(linkDirective);

        if (import.HasFlag(FederationDirectiveEnum.Key))
        {
            var keyDirective = new Directive(KEY_DIRECTIVE)
            {
                Arguments = new(
                    new QueryArgument<NonNullGraphType<StringGraphType>>() { Name = FIELDS_ARGUMENT },
                    new QueryArgument<BooleanGraphType>() { Name = RESOLVABLE_ARGUMENT, DefaultValue = true }
                ),
                Repeatable = true,
            };
            keyDirective.Locations.Add(DirectiveLocation.Object);
            keyDirective.Locations.Add(DirectiveLocation.Interface);
            schema.Directives.Register(keyDirective);
        }

        if (import.HasFlag(FederationDirectiveEnum.Shareable))
        {
            var shareableDirective = new Directive(SHAREABLE_DIRECTIVE);
            shareableDirective.Locations.Add(DirectiveLocation.FieldDefinition);
            shareableDirective.Locations.Add(DirectiveLocation.Object);
            schema.Directives.Register(shareableDirective);
        }

        if (import.HasFlag(FederationDirectiveEnum.Inaccessible))
        {
            var inaccessibleDirective = new Directive(INACCESSIBLE_DIRECTIVE);
            inaccessibleDirective.Locations.Add(DirectiveLocation.FieldDefinition);
            inaccessibleDirective.Locations.Add(DirectiveLocation.Interface);
            inaccessibleDirective.Locations.Add(DirectiveLocation.Object);
            inaccessibleDirective.Locations.Add(DirectiveLocation.Union);
            inaccessibleDirective.Locations.Add(DirectiveLocation.ArgumentDefinition);
            inaccessibleDirective.Locations.Add(DirectiveLocation.Scalar);
            inaccessibleDirective.Locations.Add(DirectiveLocation.Enum);
            inaccessibleDirective.Locations.Add(DirectiveLocation.EnumValue);
            inaccessibleDirective.Locations.Add(DirectiveLocation.InputObject);
            inaccessibleDirective.Locations.Add(DirectiveLocation.InputFieldDefinition);
            schema.Directives.Register(inaccessibleDirective);
        }

        if (import.HasFlag(FederationDirectiveEnum.Override))
        {
            var overrideDirective = new Directive(OVERRIDE_DIRECTIVE)
            {
                Arguments = new(new QueryArgument<NonNullGraphType<StringGraphType>>() { Name = FROM_ARGUMENT }),
            };
            overrideDirective.Locations.Add(DirectiveLocation.FieldDefinition);
            schema.Directives.Register(overrideDirective);
        }

        if (import.HasFlag(FederationDirectiveEnum.External))
        {
            var externalDirective = new Directive(EXTERNAL_DIRECTIVE);
            externalDirective.Locations.Add(DirectiveLocation.FieldDefinition);
            externalDirective.Locations.Add(DirectiveLocation.Object);
            schema.Directives.Register(externalDirective);
        }

        if (import.HasFlag(FederationDirectiveEnum.Provides))
        {
            var providesDirective = new Directive(PROVIDES_DIRECTIVE)
            {
                Arguments = new(new QueryArgument<NonNullGraphType<FieldSetGraphType>>() { Name = FIELDS_ARGUMENT }),
            };
            providesDirective.Locations.Add(DirectiveLocation.FieldDefinition);
            schema.Directives.Register(providesDirective);
        }

        if (import.HasFlag(FederationDirectiveEnum.Requires))
        {
            var requiresDirective = new Directive(REQUIRES_DIRECTIVE)
            {
                Arguments = new(new QueryArgument<NonNullGraphType<FieldSetGraphType>>() { Name = FIELDS_ARGUMENT }),
            };
            requiresDirective.Locations.Add(DirectiveLocation.FieldDefinition);
            schema.Directives.Register(requiresDirective);
        }
    }

    /// <summary>
    /// Applies a <c>@link</c> directive to the specified GraphQL schema based on the provided <see cref="FederationDirectiveEnum"/> values.
    /// </summary>
    /// <param name="schema">The GraphQL schema to build the link extension for.</param>
    /// <param name="version">The version of the federation specification to use, such as 2.0.</param>
    /// <param name="import">The <see cref="FederationDirectiveEnum"/> values specifying which directives to include in the link extension.</param>
    public static void ApplyLinkDirective(this ISchema schema, string version, FederationDirectiveEnum import)
    {
        schema.ApplyDirective("link", d =>
        {
            d.AddArgument(new DirectiveArgument(URL_ARGUMENT) { Value = "https://specs.apollo.dev/federation/v" + version });
            d.AddArgument(new DirectiveArgument(IMPORT_ARGUMENT)
            {
                Value = Enum.GetValues(typeof(FederationDirectiveEnum))
                    .Cast<FederationDirectiveEnum>()
                    .Where(x => import.HasFlag(x) && _federationDirectiveEnumMap.ContainsKey(x))
                    .Select(x => _federationDirectiveEnumMap[x])
                    .ToList()
            });
        });
    }
    */
}
