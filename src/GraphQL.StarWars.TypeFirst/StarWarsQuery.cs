using GraphQL.StarWars.TypeFirst.Types;

namespace GraphQL.StarWars.TypeFirst;

[Name("Query")]
public class StarWarsQuery
{
    public static async Task<IStarWarsCharacter?> HeroAsync([FromServices] StarWarsData data) => await data.GetDroidByIdAsync("3").ConfigureAwait(false);

    public static Task<Human?> HumanAsync([FromServices] StarWarsData data, string id) => data.GetHumanByIdAsync(id);

    public static Task<Droid?> DroidAsync([FromServices] StarWarsData data, string id) => data.GetDroidByIdAsync(id);
}
