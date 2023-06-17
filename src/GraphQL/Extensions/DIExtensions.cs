namespace GraphQL;

/// <summary>
/// Provides extension methods for dependency injection services.
/// </summary>
internal static class DIExtensions
{
    /// <summary>
    /// Returns <see cref="ExecutionOptions.RequestServices"/> if specified or throws <see cref="MissingRequestServicesException"/>.
    /// </summary>
    public static IServiceProvider RequestServicesOrThrow(this ExecutionOptions options)
        => options.RequestServices ?? throw new MissingRequestServicesException();

    /// <summary>
    /// Returns <see cref="Execution.IExecutionContext.RequestServices"/> if specified or throws <see cref="MissingRequestServicesException"/>.
    /// </summary>
    public static IServiceProvider RequestServicesOrThrow(this Execution.IExecutionContext context)
        => context.RequestServices ?? throw new MissingRequestServicesException();

    /// <summary>
    /// Returns <see cref="Validation.ValidationContext.RequestServices"/> if specified or throws <see cref="MissingRequestServicesException"/>.
    /// </summary>
    public static IServiceProvider RequestServicesOrThrow(this Validation.ValidationContext context)
        => context.RequestServices ?? throw new MissingRequestServicesException();

    /// <summary>
    /// Returns <see cref="IResolveFieldContext.RequestServices"/> if specified or throws <see cref="MissingRequestServicesException"/>.
    /// </summary>
    internal static IServiceProvider RequestServicesOrThrow(this IResolveFieldContext context)
        => context.RequestServices ?? throw new MissingRequestServicesException();
}
