using System;

namespace GraphQL
{
    /// <summary>
    /// Basic DependencyResolver
    /// </summary>
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
    /// <seealso cref="GraphQL.IDependencyResolver" />
    public class DefaultDependencyResolver : IDependencyResolver
    {
        /// <summary>
        /// Resolves the specified type.
        /// </summary>
        /// <typeparam name="T">Desired type</typeparam>
        /// <returns>T.</returns>
        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        /// <summary>
        /// Resolves the specified type.
        /// </summary>
        /// <param name="type">Desired type</param>
        /// <returns>An instance of <paramref name="type"/>.</returns>
        public object Resolve(Type type)
        {
            try
            {
                return Activator.CreateInstance(type);
            }
            catch (Exception exception)
            {
                throw new Exception($"Failed to call Activator.CreateInstance. Type: {type.FullName}", exception);
            }
        }
    }

    /// <summary>
    /// Func based dependency resolver.
    /// </summary>
    /// <seealso cref="GraphQL.IDependencyResolver" />
    /// <remarks>This is mainly used as an adapter for other dependency resolvers such as DI frameworks.</remarks>
    public class FuncDependencyResolver : IDependencyResolver
    {
        private readonly Func<Type, object> _resolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncDependencyResolver"/> class.
        /// </summary>
        /// <param name="resolver">The resolver function.</param>
        public FuncDependencyResolver(Func<Type, object> resolver)
        {
            _resolver = resolver;
        }

        /// <summary>
        /// Resolves the specified type.
        /// </summary>
        /// <typeparam name="T">Desired type</typeparam>
        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        /// <summary>
        /// Resolves the specified type.
        /// </summary>
        /// <param name="type">Desired type</param>
        public object Resolve(Type type)
        {
            return _resolver(type);
        }
    }

    internal sealed class ServiceProviderAdapter : IDependencyResolver
    {
        private readonly IServiceProvider _services;

        public ServiceProviderAdapter(IServiceProvider services)
        {
            _services = services;
        }

        public T Resolve<T>() => (T)_services.GetService(typeof(T));

        public object Resolve(Type type) => _services.GetService(type);
    }
}
