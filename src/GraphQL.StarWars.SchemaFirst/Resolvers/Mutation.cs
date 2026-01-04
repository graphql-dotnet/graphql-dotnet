using GraphQL.StarWars.SchemaFirst.Models;
using GraphQL.Types;

namespace GraphQL.StarWars.SchemaFirst.Resolvers;

[GraphQLMetadata("Mutation")]
public class Mutation
{
    public IStarWarsCharacter CreateHuman([FromServices] StarWarsData data, HumanInput human)
        => data.AddCharacter(human);
}
