namespace GraphQL.StarWars.SchemaFirst.Types;

public class StarWarsQuery
{
    [GraphQLMetadata("hero")]
    public async Task<StarWarsCharacter?> HeroAsync([FromServices] StarWarsData data)
        => await data.GetDroidByIdAsync("3").ConfigureAwait(false);

    [GraphQLMetadata("human")]
    public Task<Human?> HumanAsync([FromServices] StarWarsData data, string id)
        => data.GetHumanByIdAsync(id);

    [GraphQLMetadata("droid")]
    public Task<Droid?> DroidAsync([FromServices] StarWarsData data, string id)
        => data.GetDroidByIdAsync(id);
}
