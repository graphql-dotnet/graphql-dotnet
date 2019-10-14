using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.Execution
{
    /// <summary>
    /// A service provider proxy, which creates a service scope upon first use
    /// </summary>
    public class ServiceProviderProxy : IServiceProvider, IDisposable
    {
        protected readonly IServiceProvider _rootServiceProvider;
        protected IServiceScope _serviceScope;
        protected IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceProviderProxy"/> class.
        /// </summary>
        /// <param name="rootServiceProvider">The root service provider.</param>
        public ServiceProviderProxy(IServiceProvider rootServiceProvider)
        {
            _rootServiceProvider = rootServiceProvider ?? throw new ArgumentNullException(nameof(rootServiceProvider));
        }

        /// <summary>
        /// Disposes of the created service scope, if necessary
        /// </summary>
        public void Dispose()
        {
            _serviceScope?.Dispose();
        }

        /// <summary>
        /// Gets the service object of the specified type, creating a scope if none has yet been created
        /// </summary>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <returns>A service object of type serviceType. -or- null if there is no service object of type serviceType.</returns>
        public object GetService(Type serviceType)
        {
            // if the root service provider supports scopes, create a scope
            // note that DefaultServiceProvider does not support scopes, but nor does it
            //   need to as it effectively always creates objects as transients
            if (_serviceProvider == null)
            {
                lock (this)
                {
                    if (_serviceProvider == null)
                    {
                        _serviceScope = _rootServiceProvider.GetService<IServiceScopeFactory>()?.CreateScope();
                        _serviceProvider = _serviceScope?.ServiceProvider ?? _rootServiceProvider;
                    }
                }
            }
            return _serviceProvider.GetService(serviceType);
        }
    }
}
