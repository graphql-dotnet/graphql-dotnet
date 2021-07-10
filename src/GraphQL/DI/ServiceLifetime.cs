namespace GraphQL.DI
{
    /// <summary>
    /// Specifies the lifetime of a service.
    /// </summary>
    public enum ServiceLifetime
    {
        /// <summary>
        /// Specifies that a single instance of the service will be created.
        /// </summary>
        Singleton,

        /// <summary>
        /// Specifies that a new instance of the service will be created for each scope.
        /// </summary>
        /// <remarks>
        /// In ASP.NET Core applications a scope is created around each server request.
        /// </remarks>
        Scoped,

        /// <summary>
        /// Specifies that a new instance of the service will be created every time it is requested.
        /// </summary>
        Transient,
    }
}
