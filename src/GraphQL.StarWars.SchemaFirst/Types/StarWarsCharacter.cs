using GraphQL.Types.Relay.DataObjects;

namespace GraphQL.StarWars.SchemaFirst.Types;

public abstract class StarWarsCharacter
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public List<string> FriendIds { get; set; } = [];

    public Episodes[] AppearsIn { get; set; } = [];

    public string Cursor { get; set; } = string.Empty;

    public IEnumerable<StarWarsCharacter> Friends([FromServices] StarWarsData data)
        => data.GetFriends(this);

    public Connection<StarWarsCharacter> FriendsConnection([FromServices] StarWarsData data)
    {
        var edges = Friends(data).Select(character => new Edge<StarWarsCharacter>
        {
            Cursor = character.Cursor,
            Node = character
        }).ToList();

        return new Connection<StarWarsCharacter>
        {
            TotalCount = edges.Count,
            Edges = edges,
            PageInfo = new PageInfo
            {
                HasNextPage = false,
                HasPreviousPage = false,
                StartCursor = edges.FirstOrDefault()?.Cursor,
                EndCursor = edges.LastOrDefault()?.Cursor
            }
        };
    }
}
