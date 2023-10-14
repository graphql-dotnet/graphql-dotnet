namespace GraphQL
{
    /// <summary>
    /// Func based service provider.
    /// </summary>
    /// <seealso cref="IServiceProvider" />
    /// <remarks>This is mainly used as an adapter for other service providers such as DI frameworks.</remarks>
    public sealed class FuncServiceProvider : IServiceProvider
    {
        private readonly Func<Type, object?> _resolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncServiceProvider"/> class.
        /// </summary>
        /// <param name="resolver">The resolver function.</param>
        public FuncServiceProvider(Func<Type, object?> resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        /// <summary>
        /// Gets an instance of the specified type. May return <see langword="null"/>. Also you can use GetRequiredService extension method.
        /// </summary>
        /// <param name="type">Desired type</param>
        public object? GetService(Type type) => _resolver(type);
    }
}
