using GraphQL.Utilities.Federation.Enums;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Utilities.Federation
{
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

        public const string FEDERATED_V1_SDL = @"
            scalar _Any
            # scalar FieldSet

            # a union of all types that use the @key directive
            # union _Entity

            #type _Service {
            #    sdl: String
            #}

            #extend type Query {
            #    _entities(representations: [_Any!]!): [_Entity]!
            #    _service: _Service!
            #}

            directive @external on FIELD_DEFINITION
            directive @requires(fields: String!) on FIELD_DEFINITION
            directive @provides(fields: String!) on FIELD_DEFINITION
            directive @key(fields: String!) on OBJECT | INTERFACE

            # this is an optional directive
            directive @extends on OBJECT | INTERFACE
        ";
        public const string FEDERATED_V2_SDL = @"
            scalar _Any
            scalar FieldSet
            directive @external on FIELD_DEFINITION | OBJECT
            directive @requires(fields: FieldSet!) on FIELD_DEFINITION
            directive @provides(fields: FieldSet!) on FIELD_DEFINITION
            directive @key(fields: FieldSet!, resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @shareable repeatable on OBJECT | FIELD_DEFINITION
            directive @inaccessible on FIELD_DEFINITION | OBJECT | INTERFACE | UNION | ARGUMENT_DEFINITION | SCALAR | ENUM | ENUM_VALUE | INPUT_OBJECT | INPUT_FIELD_DEFINITION
            directive @override(from: String!) on FIELD_DEFINITION
            # directive @composeDirective(name: String!) repeatable on SCHEMA
            # directive @interfaceObject on OBJECT
            # directive @tag(name: String!) repeatable on FIELD_DEFINITION | INTERFACE | OBJECT | UNION | ARGUMENT_DEFINITION | SCALAR | ENUM | ENUM_VALUE | INPUT_OBJECT | INPUT_FIELD_DEFINITION
            # directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
            # directive @requiresScopes(scopes: [[federation__Scope!]!]!) on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
            # directive @extends on OBJECT | INTERFACE
        ";

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

        internal static void BuildLinkExtension(this ISchema schema, FederationDirectiveEnum import, string federationVersion = "2.5")
        {
            var linkSchemaExtension = new GraphQLSchemaExtension
            {
                Directives = new GraphQLDirectives([
                    new GraphQLDirective(new GraphQLName("link"))
                    {
                        Arguments = new GraphQLArguments([
                            new GraphQLArgument(new GraphQLName("url"), new GraphQLStringValue("https://specs.apollo.dev/federation/v" + federationVersion)),
                            new GraphQLArgument(new GraphQLName("import"), new GraphQLListValue()
                            {
                                Values = Enum.GetValues(typeof(FederationDirectiveEnum))
                                                    .Cast<FederationDirectiveEnum>()
                                                    .Where(x => import.HasFlag(x) && x != FederationDirectiveEnum.None)
                                                    .Select(x => new GraphQLStringValue(FederationDirectiveEnumMap[x]))
                                                    .Cast<GraphQLValue>()
                                                    .ToList()
                            }),
                        ])
                    }
                ])
            };
            schema.Metadata[LINK_SCHEMA_EXTENSION_METADATA] = linkSchemaExtension;
        }

        public static IHasDirectivesNode BuildAstMetadata(this IProvideMetadata type)
        {
            var astMetadata = type.GetMetadata<IHasDirectivesNode>(AST_METAFIELD, () => new GraphQLObjectTypeDefinition(new GraphQLName(AST_METAFIELD))
            {
                Directives = new GraphQLDirectives([])
            });
            type.Metadata[AST_METAFIELD] = astMetadata;
            return astMetadata;
        }

        public static void AddFieldsArgument(this GraphQLDirective directive, string fields)
        {
            ((directive.Arguments ??= new GraphQLArguments([])).Items ??= new()).Add(new GraphQLArgument(new GraphQLName(FIELDS_ARGUMENT), new GraphQLStringValue(fields)));
        }

        public static void AddFromArgument(this GraphQLDirective directive, string from)
        {
            ((directive.Arguments ??= new GraphQLArguments([])).Items ??= new()).Add(new GraphQLArgument(new GraphQLName(FROM_ARGUMENT), new GraphQLStringValue(from)));
        }

        public static void AddResolvableArgument(this GraphQLDirective directive, bool resolvable)
        {
            if (!resolvable)
            {
                ((directive.Arguments ??= new GraphQLArguments([])).Items ??= new()).Add(new GraphQLArgument(new GraphQLName(RESOLVABLE_ARGUMENT), new GraphQLFalseBooleanValue()));
            }
        }
    }
}
