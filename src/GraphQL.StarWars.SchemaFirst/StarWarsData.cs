using System.Text;
using GraphQL.StarWars.SchemaFirst.Types;

namespace GraphQL.StarWars.SchemaFirst;

public class StarWarsData
{
    private readonly List<StarWarsCharacter> _characters = [];

    public StarWarsData()
    {
        _characters.Add(new Human
        {
            Id = "1",
            Name = "Luke",
            FriendIds = ["3", "4"],
            AppearsIn = [Episodes.NEWHOPE, Episodes.EMPIRE, Episodes.JEDI],
            HomePlanet = "Tatooine",
            Cursor = "MQ=="
        });
        _characters.Add(new Human
        {
            Id = "2",
            Name = "Vader",
            AppearsIn = [Episodes.NEWHOPE, Episodes.EMPIRE, Episodes.JEDI],
            HomePlanet = "Tatooine",
            Cursor = "Mg=="
        });
        _characters.Add(new Droid
        {
            Id = "3",
            Name = "R2-D2",
            FriendIds = ["1", "4"],
            AppearsIn = [Episodes.NEWHOPE, Episodes.EMPIRE, Episodes.JEDI],
            PrimaryFunction = "Astromech",
            Cursor = "Mw=="
        });
        _characters.Add(new Droid
        {
            Id = "4",
            Name = "C-3PO",
            AppearsIn = [Episodes.NEWHOPE, Episodes.EMPIRE, Episodes.JEDI],
            PrimaryFunction = "Protocol",
            Cursor = "NA=="
        });
    }

    public IEnumerable<StarWarsCharacter> GetFriends(StarWarsCharacter character)
        => _characters.Where(c => character.FriendIds.Contains(c.Id)).ToList();

    public Human AddCharacter(HumanInput human)
    {
        var character = new Human
        {
            Id = (_characters.Count + 1).ToString(),
            Name = human.Name,
            HomePlanet = human.HomePlanet,
        };
        character.Cursor = Convert.ToBase64String(Encoding.UTF8.GetBytes(character.Id));
        _characters.Add(character);
        return character;
    }

    public Task<Human?> GetHumanByIdAsync(string id)
        => Task.FromResult<Human?>(_characters.OfType<Human>().FirstOrDefault(c => c.Id == id));

    public Task<Droid?> GetDroidByIdAsync(string id)
        => Task.FromResult<Droid?>(_characters.OfType<Droid>().FirstOrDefault(c => c.Id == id));

    public Task<List<StarWarsCharacter>> GetCharactersAsync(List<string> ids)
        => Task.FromResult(_characters.Where(c => ids.Contains(c.Id)).ToList());
}
