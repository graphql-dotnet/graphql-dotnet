using System;

namespace GraphQL
{
    /// <summary>
    /// Activator.CreateInstance based service provider.
    /// </summary>
    /// <seealso cref="IServiceProvider" />
    public sealed class DefaultServiceProvider : IServiceProvider
    {
        /// <summary>
        /// Gets an instance of the specified type. Can not return <see langword="null"/> but may throw exception.
        /// </summary>
        /// <param name="serviceType">Desired type</param>
        /// <returns>An instance of <paramref name="serviceType"/>.</returns>
        public object GetService(Type serviceType)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

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
}
