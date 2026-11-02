namespace GraphQL.StarWars.SchemaFirst.Types;

public class StarWarsQuery
{
    public async Task<StarWarsCharacter?> Hero([FromServices] StarWarsData data)
        => await data.GetDroidByIdAsync("3").ConfigureAwait(false);

    public Task<Human?> Human([FromServices] StarWarsData data, string id)
        => data.GetHumanByIdAsync(id);

    public Task<Droid?> Droid([FromServices] StarWarsData data, string id)
        => data.GetDroidByIdAsync(id);
}
