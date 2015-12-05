using GraphQL.Types;

namespace GraphQL.Tests
{
    public class StarWarsTestBase : QueryTestBase<StarWarsSchema>
    {
        public StarWarsTestBase()
        {
            Services.Singleton(new StarWarsData());
            Services.Register<StarWarsQuery>();
            Services.Register<HumanType>();
            Services.Register<DroidType>();
            Services.Register<CharacterInterface>();

            Services.Singleton(() => new StarWarsSchema(type => (GraphType) Services.Get(type)));
        }
    }
}
