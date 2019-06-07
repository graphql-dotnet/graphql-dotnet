using System;
using GraphQL.StarWars.IoC;

namespace GraphQL.Tests
{
    public static class ServiceProviderExtensions
    {
        public static T Resolve<T>(this IServiceProvider services)
        {
            return (T)services.GetService(typeof(T));
        }
    }

    public class SimpleContainerAdapater : IServiceProvider
    {
        private ISimpleContainer _container;

        public SimpleContainerAdapater(ISimpleContainer container)
        {
            _container = container;
        }

        public object GetService(Type serviceType)
        {
            return _container.Get(serviceType);
        }
    }
}
