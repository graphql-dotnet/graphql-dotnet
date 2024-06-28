using GraphQL.Types;

namespace GraphQL.Federation.Types;

/// <summary>
/// Represents an object type for services in GraphQL Federation.
/// Used to expose the SDL (Schema Definition Language) of a federation subgraph.
/// The name of this graph type is "_Service".
/// </summary>
/// <remarks>
/// Be sure to register this graph type with a <see cref="DI.ServiceLifetime.Transient">transient</see> lifetime
/// so that each schema will have it's own instance.
/// </remarks>
public class ServiceGraphType : ObjectGraphType
{
    private static readonly FederationPrintOptions _defaultPrintOptions = new() { IncludeImportedDefinitions = false };
    private string? _sdl;

    /// <inheritdoc cref="ServiceGraphType"/>
    public ServiceGraphType()
        : this(_defaultPrintOptions)
    {
    }

    /// <inheritdoc cref="ServiceGraphType"/>
    /// <param name="printOptions">Optional print options for schema printing.</param>
    public ServiceGraphType(FederationPrintOptions printOptions)
    {
        Name = "_Service";

        Field<StringGraphType>("sdl")
            .Resolve(context => _sdl ??= context.Schema.Print(printOptions));
    }
}
