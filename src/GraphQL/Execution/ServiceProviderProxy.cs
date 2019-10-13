using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.Execution
{
    public sealed class ServiceProviderProxy : IServiceProvider, IDisposable
    {
        private readonly IServiceProvider _rootServiceProvider;
        private IServiceScope _serviceScope;
        private IServiceProvider _serviceProvider;

        public ServiceProviderProxy(IServiceProvider rootServiceProvider)
        {
            _rootServiceProvider = rootServiceProvider ?? throw new ArgumentNullException(nameof(rootServiceProvider));
        }

        public void Dispose()
        {
            _serviceScope?.Dispose();
        }

        object IServiceProvider.GetService(Type serviceType)
        {
            if (_serviceProvider == null)
            {
                _serviceScope = _rootServiceProvider.CreateScope();
                _serviceProvider = _serviceScope.ServiceProvider;
            }
            return _serviceProvider.GetService(serviceType);
        }
    }
}
