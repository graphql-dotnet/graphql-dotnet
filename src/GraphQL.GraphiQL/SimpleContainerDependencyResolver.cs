using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Dependencies;
using GraphQL.StarWars.IoC;

namespace GraphQL.GraphiQL
{
    public class SimpleContainerDependencyResolver : System.Web.Http.Dependencies.IDependencyResolver
    {
        private readonly ISimpleContainer _container;

        public SimpleContainerDependencyResolver(ISimpleContainer container)
        {
            _container = container;
        }

        public object GetService(Type serviceType)
        {
            try
            {
                return _container.Get(serviceType);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return Enumerable.Empty<object>();
        }

        public IDependencyScope BeginScope()
        {
            return this;
        }

        public void Dispose()
        {
        }
    }
}
