using GraphQL.StarWars.SchemaFirst.Models;
using GraphQL.Types;
using GraphQL.Types.Relay.DataObjects;

namespace GraphQL.StarWars.SchemaFirst.Resolvers;

[GraphQLMetadata("Character")]
public class Character
{
    public IEnumerable<IStarWarsCharacter> Friends(
        [FromServices] StarWarsData data,
        [FromSource] IStarWarsCharacter character)
        => data.GetFriends(character);

    public Connection<IStarWarsCharacter> FriendsConnection(
        [FromServices] StarWarsData data,
        [FromSource] IStarWarsCharacter character,
        [FromUserContext] IResolveFieldContext context,
        int? first,
        string? after,
        int? last,
        string? before)
    {
        var friends = data.GetFriends(character).ToList();
        
        return ConnectionUtils.ToConnection(
            friends,
            context,
            first,
            after,
            last,
            before);
    }
}
