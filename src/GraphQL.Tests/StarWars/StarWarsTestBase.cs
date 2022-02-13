using GraphQL.DI;
using GraphQL.StarWars;
using GraphQL.StarWars.Types;

namespace GraphQL.Tests.StarWars
{
    public class StarWarsTestBase : QueryTestBase<StarWarsSchema>
    {
        public override void RegisterServices(IServiceRegister Services)
        {
            Services.Singleton(new StarWarsData());
            Services.Register<StarWarsQuery>();
            Services.Register<StarWarsMutation>();
            Services.Register<HumanType>();
            Services.Register<HumanInputType>();
            Services.Register<DroidType>();
            Services.Register<CharacterInterface>();
            Services.Register<EpisodeEnum>();

            Services.Singleton<StarWarsSchema>();
        }
    }
}
