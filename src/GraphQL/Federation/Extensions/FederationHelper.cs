using System.Collections;
using GraphQL.Federation.Enums;
using GraphQL.Federation.Types;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Federation.Extensions;

internal static class FederationHelper
{
    public const string AST_METAFIELD = "__AST_MetaField__";
    public const string RESOLVER_METADATA = "__FedResolver__";
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

    public static readonly Dictionary<FederationDirectiveEnum, string> FederationDirectiveEnumMap = new()
    {
        [FederationDirectiveEnum.Key] = $"@{KEY_DIRECTIVE}",
        [FederationDirectiveEnum.Shareable] = $"@{SHAREABLE_DIRECTIVE}",
        [FederationDirectiveEnum.Inaccessible] = $"@{INACCESSIBLE_DIRECTIVE}",
        [FederationDirectiveEnum.Override] = $"@{OVERRIDE_DIRECTIVE}",
        [FederationDirectiveEnum.External] = $"@{EXTERNAL_DIRECTIVE}",
        [FederationDirectiveEnum.Provides] = $"@{PROVIDES_DIRECTIVE}",
        [FederationDirectiveEnum.Requires] = $"@{REQUIRES_DIRECTIVE}",
    };

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

    public static void BuildLinkExtension(this ISchema schema, FederationDirectiveEnum import)
    {
        schema.ApplyDirective("link", d =>
        {
            d.AddArgument(new DirectiveArgument(URL_ARGUMENT) { Value = "https://specs.apollo.dev/federation/v2.0" });
            d.AddArgument(new DirectiveArgument(IMPORT_ARGUMENT)
            {
                Value = Enum.GetValues(typeof(FederationDirectiveEnum))
                    .Cast<FederationDirectiveEnum>()
                    .Where(x => import.HasFlag(x) && FederationDirectiveEnumMap.ContainsKey(x))
                    .Select(x => FederationDirectiveEnumMap[x])
                    .ToList()
            });
        });
    }

    public static IHasDirectivesNode BuildAstMetadata(this IProvideMetadata type)
    {
        var astMetadata = type.GetMetadata<IHasDirectivesNode>(AST_METAFIELD, () => new GraphQLObjectTypeDefinition(new("dummy"))
        {
            Directives = new(new())
        });
        type.Metadata[AST_METAFIELD] = astMetadata;
        return astMetadata;
    }

    public static void AddFieldsArgument(this GraphQLDirective directive, string fields)
    {
        (directive.Arguments ??= new([])).Items.Add(new(new(FIELDS_ARGUMENT), new GraphQLStringValue(fields)));
    }

    public static void AddFromArgument(this GraphQLDirective directive, string from)
    {
        (directive.Arguments ??= new([])).Items.Add(new(new(FROM_ARGUMENT), new GraphQLStringValue(from)));
    }

    public static void AddResolvableArgument(this GraphQLDirective directive, bool resolvable)
    {
        if (!resolvable)
        {
            (directive.Arguments ??= new([])).Items.Add(new(new(RESOLVABLE_ARGUMENT), new GraphQLFalseBooleanValue()));
        }
    }

    internal static void AddFederationFields(this ISchema schema)
    {
        var type = schema.Query
            ?? throw new InvalidOperationException("The query type for the schema has not been defined.");

        type.AddField(new FieldType
        {
            Name = "_service",
            ResolvedType = new NonNullGraphType(new GraphQLTypeReference("_Service")),
            Resolver = new FuncFieldResolver<object>(_ => BoolBox.True)
        });

        var representationsArgumentGraphType = new NonNullGraphType(new ListGraphType(new NonNullGraphType(new GraphQLTypeReference("_Any"))));
        var representationsArgument = new QueryArgument(representationsArgumentGraphType) { Name = "representations" };
        representationsArgument.Validator += (value) => EntityResolver.Instance.ConvertRepresentations(schema, (IList)value);
        type.AddField(new FieldType
        {
            Name = "_entities",
            ResolvedType = new NonNullGraphType(new ListGraphType(new GraphQLTypeReference("_Entity"))),
            Arguments = new QueryArguments(
                new QueryArgument(representationsArgumentGraphType) { Name = "representations" }
            ),
            Resolver = EntityResolver.Instance,
        });
    }
}
