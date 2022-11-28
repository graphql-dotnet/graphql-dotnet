using GraphQL.StarWars.Types;

namespace GraphQL.StarWars;

public class StarWarsData
{
    private readonly List<StarWarsCharacter> _characters = new();

    public StarWarsData()
    {
        _characters.Add(new Human
        {
            Id = "1",
            Name = "Luke",
            Friends = new List<string> { "3", "4" },
            AppearsIn = new[] { 4, 5, 6 },
            HomePlanet = "Tatooine",
            Cursor = "MQ=="
        });
        _characters.Add(new Human
        {
            Id = "2",
            Name = "Vader",
            AppearsIn = new[] { 4, 5, 6 },
            HomePlanet = "Tatooine",
            Cursor = "Mg=="
        });

        _characters.Add(new Droid
        {
            Id = "3",
            Name = "R2-D2",
            Friends = new List<string> { "1", "4" },
            AppearsIn = new[] { 4, 5, 6 },
            PrimaryFunction = "Astromech",
            Cursor = "Mw=="
        });
        _characters.Add(new Droid
        {
            Id = "4",
            Name = "C-3PO",
            AppearsIn = new[] { 4, 5, 6 },
            PrimaryFunction = "Protocol",
            Cursor = "NA=="
        });
    }

    public IEnumerable<StarWarsCharacter> GetFriends(StarWarsCharacter character)
    {
        if (character == null)
        {
            return null;
        }

        var friends = new List<StarWarsCharacter>();
        var lookup = character.Friends;
        if (lookup != null)
        {
            foreach (var c in _characters.Where(h => lookup.Contains(h.Id)))
                friends.Add(c);
        }
        return friends;
    }

    public StarWarsCharacter AddCharacter(StarWarsCharacter character)
    {
        character.Id = _characters.Count.ToString();
        _characters.Add(character);
        return character;
    }

    public Task<Human> GetHumanByIdAsync(string id)
    {
        return Task.FromResult(_characters.FirstOrDefault(h => h.Id == id && h is Human) as Human);
    }

    public Task<Droid> GetDroidByIdAsync(string id)
    {
        return Task.FromResult(_characters.FirstOrDefault(h => h.Id == id && h is Droid) as Droid);
    }

    public Task<List<StarWarsCharacter>> GetCharactersAsync(List<string> guids)
    {
        return Task.FromResult(_characters.Where(c => guids.Contains(c.Id)).ToList());
    }
}
