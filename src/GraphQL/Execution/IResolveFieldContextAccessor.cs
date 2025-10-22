namespace GraphQL.Execution;

/// <summary>
/// Provides access to the current <see cref="IResolveFieldContext"/> during field resolution.
/// This is similar to <c>IHttpContextAccessor</c> in ASP.NET Core.
/// </summary>
public interface IResolveFieldContextAccessor
{
    /// <summary>
    /// Gets or sets the current <see cref="IResolveFieldContext"/>.
    /// Returns <see langword="null"/> if no field is currently being resolved or if
    /// <see cref="Types.Schema.ResolveFieldContextAccessor"/> is not enabled.
    /// </summary>
    public IResolveFieldContext? Context { get; set; }
}
