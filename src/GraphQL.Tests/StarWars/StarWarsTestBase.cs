using GraphQL.Execution;
using GraphQL.StarWars;
using GraphQL.StarWars.Types;

namespace GraphQL.Tests.StarWars
{
    public class StarWarsTestBase : QueryTestBase<StarWarsSchema, GraphQLDocumentBuilder>
    {
        public StarWarsTestBase()
        {
            Services.Singleton(new StarWarsData());
            Services.Register<StarWarsQuery>();
            Services.Register<HumanType>();
            Services.Register<DroidType>();
            Services.Register<CharacterInterface>();

            Services.Singleton(new StarWarsSchema(new FuncDependencyResolver(type => Services.Get(type))));
        }
    }
}
