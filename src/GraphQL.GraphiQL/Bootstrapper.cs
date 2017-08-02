using GraphQL.Http;
using GraphQL.StarWars;
using GraphQL.StarWars.IoC;
using GraphQL.StarWars.Types;

namespace GraphQL.GraphiQL
{
    public class Bootstrapper
    {
        public System.Web.Http.Dependencies.IDependencyResolver Resolver()
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
            container.Register<StarWarsMutation>();
            container.Register<HumanType>();
            container.Register<HumanInputType>();
            container.Register<DroidType>();
            container.Register<CharacterInterface>();
            container.Singleton(new StarWarsSchema(new FuncDependencyResolver(type => container.Get(type))));

            return container;
        }
    }
}
