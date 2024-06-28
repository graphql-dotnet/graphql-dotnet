using GraphQL.Utilities;

namespace GraphQL.Federation.Types;

/// <summary>
/// Provides options for printing the schema definition language (SDL) 
/// returned by '<c>services { sdl }</c>' in a GraphQL federation subgraph.
/// </summary>
/// <remarks>
/// By default this does not include types or directives imported by '@link'.
/// Please disable <see cref="PrintOptions.IncludeFederationTypes"/> for
/// Federation v1 compatibility.
/// </remarks>
public class FederationPrintOptions : PrintOptions
{
    /// <inheritdoc cref="FederationPrintOptions"/>
    public FederationPrintOptions()
    {
        IncludeImportedDefinitions = false;
    }
}
