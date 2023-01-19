using System.Text;
using GraphQL.StarWars.TypeFirst.Types;

namespace GraphQL.StarWars.TypeFirst;

public class StarWarsData
{
    private readonly List<IStarWarsCharacter> _characters = new();

    public StarWarsData()
    {
        _characters.Add(new Human
        {
            Id = "1",
            Name = "Luke",
            Friends = new List<string> { "3", "4" },
            AppearsIn = new[] { Episodes.NEWHOPE, Episodes.EMPIRE, Episodes.JEDI },
            HomePlanet = "Tatooine",
            Cursor = "MQ=="
        });
        _characters.Add(new Human
        {
            Id = "2",
            Name = "Vader",
            AppearsIn = new[] { Episodes.NEWHOPE, Episodes.EMPIRE, Episodes.JEDI },
            HomePlanet = "Tatooine",
            Cursor = "Mg=="
        });

        _characters.Add(new Droid
        {
            Id = "3",
            Name = "R2-D2",
            Friends = new List<string> { "1", "4" },
            AppearsIn = new[] { Episodes.NEWHOPE, Episodes.EMPIRE, Episodes.JEDI },
            PrimaryFunction = "Astromech",
            Cursor = "Mw=="
        });
        _characters.Add(new Droid
        {
            Id = "4",
            Name = "C-3PO",
            AppearsIn = new[] { Episodes.NEWHOPE, Episodes.EMPIRE, Episodes.JEDI },
            PrimaryFunction = "Protocol",
            Cursor = "NA=="
        });
    }

    public IEnumerable<IStarWarsCharacter> GetFriends(IStarWarsCharacter character)
    {
        var friends = new List<IStarWarsCharacter>();
        var lookup = character.Friends;
        if (lookup != null)
        {
            foreach (var c in _characters.Where(h => lookup.Contains(h.Id)))
                friends.Add(c);
        }
        return friends;
    }

    public IStarWarsCharacter AddCharacter(HumanInput human)
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
    {
        return Task.FromResult(_characters.FirstOrDefault(h => h.Id == id && h is Human) as Human);
    }

    public Task<Droid?> GetDroidByIdAsync(string id)
    {
        return Task.FromResult(_characters.FirstOrDefault(h => h.Id == id && h is Droid) as Droid);
    }

    public Task<List<IStarWarsCharacter>> GetCharactersAsync(List<string> ids)
    {
        return Task.FromResult(_characters.Where(c => ids.Contains(c.Id)).ToList());
    }
}
