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
    public const string REPRESENTATIONS_ARGUMENT = "representations";
}
