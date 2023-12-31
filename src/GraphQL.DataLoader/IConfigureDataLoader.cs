using GraphQL.DI;

namespace GraphQL.DataLoader;

/// <summary>
/// Provides an interface to configure data loaders within GraphQL.NET.
/// </summary>
/// <remarks>
/// This interface is typically used for setting up and configuring data loaders,
/// allowing for customization and extension of the GraphQL data loading process.
/// It grants access to the GraphQL builder for further configurations.
/// </remarks>
public interface IConfigureDataLoader
{
    /// <inheritdoc cref="IGraphQLBuilder.Services"/>
    IServiceRegister Services { get; }
}

internal class ConfigureDataLoader : IConfigureDataLoader
{
    public ConfigureDataLoader(IServiceRegister services)
    {
        Services = services;
    }

    public IServiceRegister Services { get; }
}
