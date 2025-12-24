using System.ComponentModel;
using GraphQL.Types.Relay.DataObjects;

namespace GraphQL.StarWars.TypeFirst.Types;

[Name("Character")]
public interface IStarWarsCharacter
{
    [Description("The id of the character.")]
    public string Id { get; set; }

    [Description("The name of the character.")]
    public string Name { get; set; }

    [Ignore]
    public List<string> Friends { get; set; }

    [Name("Friends")]
    public IEnumerable<IStarWarsCharacter> GetFriends([FromServices] StarWarsData data);

    [Name("FriendsConnection")]
    public Connection<IStarWarsCharacter> GetFriendsConnection([FromServices] StarWarsData data);

    [Description("Which movie they appear in.")]
    public Episodes[] AppearsIn { get; set; }

    [Ignore]
    public string Cursor { get; set; }
}
