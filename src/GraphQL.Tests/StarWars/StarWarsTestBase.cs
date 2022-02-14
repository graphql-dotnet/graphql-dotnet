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
            Services.Transient<StarWarsQuery>();
            Services.Transient<StarWarsMutation>();
            Services.Transient<HumanType>();
            Services.Transient<HumanInputType>();
            Services.Transient<DroidType>();
            Services.Transient<CharacterInterface>();
            Services.Transient<EpisodeEnum>();

            Services.Singleton<StarWarsSchema>();
        }
    }
}
