using System;

namespace GraphQL
{
    /// <summary>
    /// Basic DependencyResolver
    /// </summary>
    [Obsolete("Use IServiceProvider instead")]
    public interface IDependencyResolver
    {
        /// <summary>
        /// Resolves the specified type.
        /// </summary>
        /// <typeparam name="T">Desired type</typeparam>
        T Resolve<T>();

        /// <summary>
        /// Resolves the specified type.
        /// </summary>
        /// <param name="type">Desired type</param>
        object Resolve(Type type);
    }

    /// <summary>
    /// Dependency resolver based on Activator.CreateInstance
    /// </summary>
    /// <seealso cref="System.IServiceProvider" />
    public sealed class DefaultServiceProvider : IServiceProvider
    {
        /// <summary>
        /// Gets the specified service type.
        /// </summary>
        /// <param name="serviceType">Desired type</param>
        /// <returns>An instance of <paramref name="serviceType"/>.</returns>
        public object GetService(Type serviceType)
        {
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

    internal sealed class DependencyResolverToServiceProviderAdapter : IServiceProvider
    {
        private readonly IDependencyResolver _resolver;

        public DependencyResolverToServiceProviderAdapter(IDependencyResolver resolver)
        {
            _resolver = resolver;
        }

        public object GetService(Type serviceType)
        {
            return _resolver.Resolve(serviceType);
        }
    }

    /// <summary>
    /// Func based service provider.
    /// </summary>
    /// <seealso cref="System.IServiceProvider" />
    /// <remarks>This is mainly used as an adapter for other dependency resolvers such as DI frameworks.</remarks>
    [Obsolete("Use GraphQL.FuncServiceProvider instead.")]
    public sealed class FuncDependencyResolver : IServiceProvider
    {
        private readonly FuncServiceProvider _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncDependencyResolver"/> class.
        /// </summary>
        /// <param name="resolver">The resolver function.</param>
        public FuncDependencyResolver(Func<Type, object> resolver)
        {
            _provider = new FuncServiceProvider(resolver);
        }

        /// <summary>
        /// Resolves the specified type.
        /// </summary>
        /// <param name="type">Desired type</param>
        public object GetService(Type type)
        {
            return _provider.GetService(type);
        }
    }

    /// <summary>
    /// Func based service provider.
    /// </summary>
    /// <seealso cref="System.IServiceProvider" />
    /// <remarks>This is mainly used as an adapter for other service providers such as DI frameworks.</remarks>
    public sealed class FuncServiceProvider : IServiceProvider
    {
        private readonly Func<Type, object> _resolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncServiceProvider"/> class.
        /// </summary>
        /// <param name="resolver">The resolver function.</param>
        public FuncServiceProvider(Func<Type, object> resolver)
        {
            _resolver = resolver;
        }

        /// <summary>
        /// Gets an instance of the specified type.
        /// </summary>
        /// <param name="type">Desired type</param>
        public object GetService(Type type)
        {
            return _resolver(type);
        }
    }
}
