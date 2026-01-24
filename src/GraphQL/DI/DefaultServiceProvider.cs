namespace GraphQL;

/// <summary>
/// Activator.CreateInstance based service provider.
/// </summary>
/// <seealso cref="IServiceProvider" />
public sealed class DefaultServiceProvider : IServiceProvider
{
    /// <inheritdoc cref="DefaultServiceProvider"/>
    [RequiresUnreferencedCode("This class uses Activator.CreateInstance which requires access to the target type's constructor.")]
    public DefaultServiceProvider() { }

    /// <summary>
    /// Gets an instance of the specified type. Returns <see langword="null"/> for interfaces.
    /// Can not return <see langword="null"/> for classes but may throw exception.
    /// </summary>
    /// <param name="serviceType">Desired type</param>
    /// <returns>An instance of <paramref name="serviceType"/>.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Constructor is marked with RequiresUnreferencedCode")]
    public object? GetService(Type serviceType)
    {
        if (serviceType == null)
            throw new ArgumentNullException(nameof(serviceType));

        if (serviceType == typeof(IServiceProvider) || serviceType == typeof(DefaultServiceProvider))
            return this;

        if (serviceType.IsInterface || serviceType.IsAbstract || serviceType.IsGenericTypeDefinition)
            return null;

        try
        {
            return Activator.CreateInstance(serviceType);
        }
        catch (Exception exception)
        {
            throw new Exception($"Failed to call Activator.CreateInstance. Type: {serviceType.FullName}", exception);
        }
    }
}
