using System.ComponentModel;
using GraphQL.Types.Relay.DataObjects;

namespace GraphQL.StarWars.TypeFirst.Types;

[Name("Character")]
public interface IStarWarsCharacter
{
    [Description("The id of the character.")]
    string Id { get; set; }

    [Description("The name of the character.")]
    string Name { get; set; }

    [Ignore]
    List<string> Friends { get; set; }

    [Name("Friends")]
    IEnumerable<IStarWarsCharacter> GetFriends([FromServices] StarWarsData data);

    [Name("FriendsConnection")]
    Connection<IStarWarsCharacter> GetFriendsConnection([FromServices] StarWarsData data);

    [Description("Which movie they appear in.")]
    Episodes[] AppearsIn { get; set; }

    [Ignore]
    string Cursor { get; set; }

}
