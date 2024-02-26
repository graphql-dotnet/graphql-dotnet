using GraphQL.Federation.Enums;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Federation.Extensions;

internal static class FederationHelper
{
    public const string AST_METAFIELD = "__AST_MetaField__";
    public const string RESOLVER_METADATA = "__FedResolver__";
    public const string LINK_SCHEMA_EXTENSION_METADATA = "__FedLinkSchemaExtension__";

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


    public static void BuildLinkExtension(this ISchema schema, FederationDirectiveEnum import)
    {
        var linkSchemaExtension = new GraphQLSchemaExtension
        {
            Directives = new([
                new GraphQLDirective(new("link"))
                {
                    Arguments = new([
                        new(new("url"), new GraphQLStringValue("https://specs.apollo.dev/federation/v2.0")),
                        new(new("import"), new GraphQLListValue()
                        {
                            Values = Enum.GetValues(typeof(FederationDirectiveEnum))
                                .Cast<FederationDirectiveEnum>()
                                .Where(x => import.HasFlag(x))
                                .Select(x => new GraphQLStringValue(FederationDirectiveEnumMap[x]))
                                .Cast<GraphQLValue>()
                                .ToList()
                        })
                    ])
                }
            ])
        };
        schema.Metadata[LINK_SCHEMA_EXTENSION_METADATA] = linkSchemaExtension;
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
}
