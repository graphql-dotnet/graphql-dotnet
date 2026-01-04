using GraphQL.StarWars.SchemaFirst.Models;
using GraphQL.Types;

namespace GraphQL.StarWars.SchemaFirst.Resolvers;

[GraphQLMetadata("Query")]
public class Query
{
    public async Task<IStarWarsCharacter?> Hero([FromServices] StarWarsData data)
        => await data.GetDroidByIdAsync("3").ConfigureAwait(false);

    public Task<Human?> Human([FromServices] StarWarsData data, string id)
        => data.GetHumanByIdAsync(id);

    public Task<Droid?> Droid([FromServices] StarWarsData data, string id)
        => data.GetDroidByIdAsync(id);
}
