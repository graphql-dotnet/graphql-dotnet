using System.Collections;
using GraphQL.Types;
using GraphQL.Types.Scalars;

namespace GraphQL.Utilities;

/// <summary>
/// Configuration for the @link directive, allowing customization of the linked schema.
/// </summary>
public sealed class LinkConfiguration
{
    internal const string LINK_URL = "https://specs.apollo.dev/link/v1.0";
    internal const string METADATA_KEY = "__Link_Configuration";

    /// <summary>
    /// Initializes a new instance of the <see cref="LinkConfiguration"/> class with the specified URL.
    /// </summary>
    public LinkConfiguration(string url)
    {
        Url = url;
    }

    /// <summary>
    /// The URL of the linked schema.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// The namespace prefix that imported schema elements are placed in.
    /// Defaults to the name of the imported schema, as derived from the URL.
    /// </summary>
    public string? DefaultNamespacePrefix { get; set; }

    /// <summary>
    /// The purpose of the link, which can influence how the link is treated in the schema.
    /// </summary>
    public LinkPurpose? Purpose { get; set; }

    /// <summary>
    /// A list of elements to import from the linked schema, with optional aliases.
    /// </summary>
    public List<LinkImport> Imports { get; set; } = new();

    /// <summary>
    /// Returns the aliased name for the requested type.
    /// For types that have been explicitly imported, it returns the configured alias, or when no alias is configured, the type name.
    /// For other types, it returns the type name prefixed with the default namespace prefix.
    /// </summary>
    public string NameForType(string typeName)
    {
        foreach (var import in Imports)
        {
            if (import.Name == typeName)
                return import.Alias ?? typeName;
        }
        if (DefaultNamespacePrefix == null)
            throw new InvalidOperationException("The specified type has not been imported.");
        return DefaultNamespacePrefix + "__" + typeName;
    }

    /// <summary>
    /// Returns the aliased name for the requested directive.
    /// For directives that have been explicitly imported, it returns the configured alias, or when no alias is configured, the directive name.
    /// For other directives, it returns the directive name prefixed with the default namespace prefix.
    /// </summary>
    /// <remarks>
    /// Does not prefix the returned directive name with '@'.
    /// </remarks>
    /// <param name="directiveName">The name of the directive to get the name for, without the '@' prefix.</param>
    public string NameForDirective(string directiveName)
    {
        var withAt = "@" + directiveName;
        foreach (var import in Imports)
        {
            if (import.Name == withAt)
                return import.Alias?.Substring(1) ?? directiveName;
        }
        if (DefaultNamespacePrefix == null)
            throw new InvalidOperationException("The specified type has not been imported.");
        return DefaultNamespacePrefix + "__" + directiveName;
    }

    /// <summary>
    /// Configures the specified applied directive with the settings from this configuration.
    /// </summary>
    /// <remarks>
    /// This is called by <see cref="SchemaExtensions.LinkSchema(ISchema, LinkConfiguration)"/> to apply the configuration to the schema.
    /// </remarks>
    internal void ConfigureAppliedDirective(AppliedDirective o)
    {
        if (o.Name != "link")
            throw new ArgumentException("The supplied directive is not a @link directive.", nameof(o));

        o.WithMetadata(METADATA_KEY, this);

        o.AddArgument(new DirectiveArgument("url") { Value = Url ?? throw new InvalidOperationException("No url specified.") });

        // parse the url now so that future calls to NameForType and NameForDirective don't need to re-parse it
        var parsedUrl = TryParseUrl(Url);

        if (DefaultNamespacePrefix != null)
        {
            if (DefaultNamespacePrefix != parsedUrl.Name)
            {
                if (DefaultNamespacePrefix == "")
                    throw new InvalidOperationException("The default namespace prefix cannot be an empty string.");
                NameValidator.ValidateName(DefaultNamespacePrefix, NamedElement.Type);
                o.AddArgument(new DirectiveArgument("as") { Value = DefaultNamespacePrefix });
            }
        }
        else
        {
            DefaultNamespacePrefix = parsedUrl.Name;
        }

        if (Purpose != null)
            o.AddArgument(new DirectiveArgument("purpose") { Value = Purpose.Value });

        if (Imports.Count > 0)
        {
            var importList = new List<object>();
            foreach (var import in Imports)
            {
                if (import == null)
                    continue;
                if (import.Name == "")
                    throw new InvalidOperationException("No name specified for an import.");
                if (import.Name.StartsWith("@", StringComparison.Ordinal))
                {
                    NameValidator.ValidateName(import.Name.Substring(1), NamedElement.Directive);
                    if (import.Alias != null)
                    {
                        if (!import.Alias.StartsWith("@", StringComparison.Ordinal))
                            throw new InvalidOperationException("An alias for a directive must start with '@'.");
                        NameValidator.ValidateName(import.Alias.Substring(1), NamedElement.Directive);
                    }
                }
                else
                {
                    NameValidator.ValidateName(import.Name, NamedElement.Type);
                }
                if (import.Alias != null && import.Name != import.Alias)
                {
                    importList.Add(new Dictionary<string, string> { { "name", import.Name }, { "as", import.Alias } });
                }
                else
                {
                    importList.Add(import.Name);
                }
            }
            o.AddArgument(new DirectiveArgument("import") { Value = importList });
        }
    }

    /// <summary>
    /// This parses the URL within the @link directive to extract the name and version of the linked schema.
    /// The URL is parsed based on the @link specification.
    /// This will not throw an exception, but returns null values if the URL cannot be parsed.
    /// </summary>
    private static (string? Name, string? Version) TryParseUrl(string url)
    {
        // Check if the URL is a valid RFC 3986 URL
        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            return (null, null);
        }

        var uri = new Uri(url);
        string[] segments = uri.AbsolutePath.TrimEnd('/').Split('/');

        if (segments.Length < 2)
        {
            return (null, null);
        }

        string? version = segments[segments.Length - 1]; // Last segment
        string? name = segments[segments.Length - 2]; // Second to last segment

        // Check if the last segment is a valid version tag
        if (!IsValidVersionTag(version))
        {
            version = null;
        }

        // Check if the second to last segment is a valid GraphQL name
        if (!IsValidGraphQLName(name))
        {
            name = null;
        }

        return (name, version);

        static bool IsValidVersionTag(string version)
        {
            // A simple check if the version starts with 'v' and followed by digits and dots
            if (version.StartsWith("v"))
            {
                bool requireDigit = true;
                for (int i = 1; i < version.Length; i++)
                {
                    char c = version[i];
                    if ((c < '0' && c > '9') && (requireDigit || c != '.'))
                    {
                        return false;
                    }
                    requireDigit = c == '.';
                }
                return !requireDigit;
            }
            return false;
        }

        static bool IsValidGraphQLName(string name)
        {
            // Must not start or end with an underscore and must not contain "__"
            if (string.IsNullOrEmpty(name) || name.StartsWith("_") || name.EndsWith("_") || name.Contains("__"))
            {
                return false;
            }

            // Must be valid GraphQL name: not start with digit, and only: _ A-Z a-z 0-9
            bool noDigit = true;
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c == '_')) && (noDigit || (c < '0' && c > '9')))
                    return false;
                noDigit = false;
            }

            return true;
        }
    }

    /// <summary>
    /// Returns the configuration for the specified applied directive, if one was applied.
    /// </summary>
    /// <remarks>
    /// This assumes that the link directive has been configured with <see cref="ConfigureAppliedDirective"/> or
    /// parsed with <see cref="TryParseDirective"/>. If the directive was not configured or parsed, this method will return null.
    /// </remarks>
    internal static LinkConfiguration? GetConfiguration(AppliedDirective appliedDirective)
        => appliedDirective.GetMetadata<LinkConfiguration>(METADATA_KEY);

    /// <summary>
    /// Attempts to parse the specified applied directive into a <see cref="LinkConfiguration"/> instance.
    /// </summary>
    /// <remarks>
    /// Should be run during schema construction to parse the @link directive for schema-first schemas.
    /// </remarks>
    internal static bool TryParseDirective(AppliedDirective appliedDirective, bool saveToMetadata, out LinkConfiguration configuration)
    {
        configuration = appliedDirective.GetMetadata<LinkConfiguration>(METADATA_KEY);
        if (configuration != null)
            return true;
        if (appliedDirective == null || appliedDirective.Name != "link")
            return false;
        if (appliedDirective.FindArgument("url") is not { Value: string url } || url == null)
            return false;
        configuration = new LinkConfiguration(url);
        if (appliedDirective.FindArgument("as") is { Value: string defaultNamespacePrefix } && defaultNamespacePrefix != null)
            configuration.DefaultNamespacePrefix = defaultNamespacePrefix;
        else
            configuration.DefaultNamespacePrefix = TryParseUrl(url).Name;
        if (appliedDirective.FindArgument("purpose") is { Value: LinkPurpose purpose })
            configuration.Purpose = purpose;
        if (appliedDirective.FindArgument("import") is { Value: IEnumerable importList })
        {
            foreach (var import in importList)
            {
                if (import is string importName)
                {
                    configuration.Imports.Add(new LinkImport { Name = importName });
                }
                else if (import is IDictionary importDict)
                {
                    string? importName2 = null;
                    string? importAlias = null;
                    foreach (DictionaryEntry entry in importDict)
                    {
                        if (entry.Key is string key)
                        {
                            if (key == "name")
                                importName2 = entry.Value as string;
                            else if (key == "as")
                                importAlias = entry.Value as string;
                        }
                    }
                    if (importName2 != null)
                        configuration.Imports.Add(new LinkImport { Name = importName2, Alias = importAlias });
                }
            }
        }
        if (saveToMetadata)
            appliedDirective.WithMetadata(METADATA_KEY, configuration);
        return true;
    }
}
