using System.Web.Http.Dependencies;
using GraphQL.Http;
using GraphQL.StarWars;
using GraphQL.StarWars.IoC;
using GraphQL.StarWars.Types;
using GraphQL.Types;

namespace GraphQL.GraphiQL
{
    public class Bootstrapper
    {
        public IDependencyResolver Resolver()
        {
            var container = BuildContainer();
            var resolver = new SimpleContainerDependencyResolver(container);
            return resolver;
        }

        private ISimpleContainer BuildContainer()
        {
            var container = new SimpleContainer();
            container.Singleton<IDocumentExecuter>(new DocumentExecuter());
            container.Singleton<IDocumentWriter>(new DocumentWriter(true));

            container.Singleton(new StarWarsData());
            container.Register<StarWarsQuery>();
            container.Register<HumanType>();
            container.Register<DroidType>();
            container.Register<CharacterInterface>();
            container.Singleton(new StarWarsSchema(type => (GraphType) container.Get(type)));

            return container;
        }
    }
}
