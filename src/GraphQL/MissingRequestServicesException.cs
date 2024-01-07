namespace GraphQL;

/// <summary>
/// Indicates that <see cref="IResolveFieldContext.RequestServices"/> was required but not set.
/// </summary>
public class MissingRequestServicesException : InvalidOperationException
{
    /// <inheritdoc cref="MissingRequestServicesException"/>
    public MissingRequestServicesException() : base("No service provider specified. Please set the value of the ExecutionOptions.RequestServices to a valid service provider. Typically, this would be a scoped service provider from your dependency injection framework.")
    {
    }
}
