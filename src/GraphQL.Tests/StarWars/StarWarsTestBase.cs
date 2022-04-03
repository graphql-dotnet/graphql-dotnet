using GraphQL.DI;
using GraphQL.StarWars;
using GraphQL.StarWars.Types;

namespace GraphQL.Tests.StarWars;

public class StarWarsTestBase : QueryTestBase<StarWarsSchema>
{
    public override void RegisterServices(IServiceRegister register)
    {
        register.Singleton(new StarWarsData());
        register.Transient<StarWarsQuery>();
        register.Transient<StarWarsMutation>();
        register.Transient<HumanType>();
        register.Transient<HumanInputType>();
        register.Transient<DroidType>();
        register.Transient<CharacterInterface>();
        register.Transient<EpisodeEnum>();

        register.Singleton<StarWarsSchema>();
    }
}
