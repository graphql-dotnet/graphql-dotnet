using GraphQL.Utilities;

namespace GraphQL.Federation;

/// <summary>
/// Configuration settings for the AddFederation extension method.
/// </summary>
public class FederationSettings
{
    /// <summary>
    /// Indicates the version of the federation specification to use.
    /// </summary>
    public string Version { get; set; } = "2.0";

    /// <summary>
    /// Specifies what directives should be configured within the schema.
    /// Defaults to all supported federation directives based on the selected version.
    /// </summary>
    public FederationDirectiveEnum? ImportDirectives { get; set; }

    /// <summary>
    /// Configures the print options for the _service { sdl } field.
    /// By default, configured to suppress federation types if the federation version is 1.x.
    /// </summary>
    public PrintOptions? SdlPrintOptions = null;
}
