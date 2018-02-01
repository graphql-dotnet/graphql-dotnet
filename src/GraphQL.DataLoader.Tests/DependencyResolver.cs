using System;
using GraphQL;

namespace GraphQL.DataLoader.Tests
{
    public class DependencyResolver : IDependencyResolver
    {
        private readonly IServiceProvider _services;

        public DependencyResolver(IServiceProvider services)
        {
            _services = services;
        }

        public T Resolve<T>() => (T)_services.GetService(typeof(T));

        public object Resolve(Type type) => _services.GetService(type);
    }
}
