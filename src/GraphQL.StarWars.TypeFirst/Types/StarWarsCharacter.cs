using System.ComponentModel;
using GraphQL.Types.Relay.DataObjects;

namespace GraphQL.StarWars.TypeFirst.Types;

public abstract class StarWarsCharacter : IStarWarsCharacter
{
    [Description("The id of the character.")]
    public string Id { get; set; }

    [Description("The name of the character.")]
    public string Name { get; set; }

    [Ignore]
    public List<string> Friends { get; set; }

    [Name("Friends")]
    public IEnumerable<IStarWarsCharacter> GetFriends([FromServices] StarWarsData data)
        => data.GetFriends(this);

    [Name("FriendsConnection")]
    public Connection<IStarWarsCharacter> GetFriendsConnection([FromServices] StarWarsData data)
    {
        var edges = GetFriends(data).Select(x => new Edge<IStarWarsCharacter>
        {
            Cursor = x.Cursor,
            Node = x
        }).ToList();

        return new Connection<IStarWarsCharacter>()
        {
            Edges = edges,
            TotalCount = edges.Count,
            PageInfo = new PageInfo()
            {
                HasNextPage = false,
                HasPreviousPage = false,
                StartCursor = edges.FirstOrDefault()?.Cursor,
                EndCursor = edges.LastOrDefault()?.Cursor,
            }
        };
    }

    [Description("Which movie they appear in.")]
    public Episodes[] AppearsIn { get; set; }

    [Ignore]
    public string Cursor { get; set; }
}
