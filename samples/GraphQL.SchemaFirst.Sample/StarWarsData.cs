namespace GraphQL.SchemaFirst.Sample;

/// <summary>
/// Star Wars data source.
/// Returns plain C# objects (POCOs) that the Schema-First resolvers return.
/// </summary>
public class StarWarsData
{
    private readonly List<Character> _characters = [];

    public StarWarsData()
    {
        // Initialize with sample data
        _characters.Add(new Human
        {
            Id = "1000",
            Name = "Luke Skywalker",
            Friends = new List<string> { "1003", "2001" },
            AppearsIn = new List<Episode> { Episode.NEWHOPE, Episode.EMPIRE, Episode.JEDI },
            HomePlanet = "Tatooine",
            Cursor = "MTAwMA=="
        });
        _characters.Add(new Human
        {
            Id = "1003",
            Name = "Darth Vader",
            AppearsIn = new List<Episode> { Episode.NEWHOPE, Episode.EMPIRE, Episode.JEDI },
            HomePlanet = "Tatooine",
            Cursor = "MTAwMw=="
        });

        _characters.Add(new Droid
        {
            Id = "2001",
            Name = "R2-D2",
            Friends = new List<string> { "1000", "1003" },
            AppearsIn = new List<Episode> { Episode.NEWHOPE, Episode.EMPIRE, Episode.JEDI },
            PrimaryFunction = "Astromech",
            Cursor = "MjAwMQ=="
        });
        _characters.Add(new Droid
        {
            Id = "2002",
            Name = "C-3PO",
            AppearsIn = new List<Episode> { Episode.NEWHOPE, Episode.EMPIRE, Episode.JEDI },
            PrimaryFunction = "Protocol",
            Cursor = "MjAwMg=="
        });
    }

    /// <summary>
    /// Get friends of a character by their friend IDs.
    /// </summary>
    public IEnumerable<Character> GetFriends(Character character)
    {
        if (character?.Friends == null)
        {
            return Enumerable.Empty<Character>();
        }

        var friendIds = character.Friends;
        return _characters.Where(c => friendIds.Contains(c.Id)).ToList();
    }

    /// <summary>
    /// Add a character to the data source.
    /// </summary>
    public Character AddCharacter(Character character)
    {
        character.Id = (_characters.Count + 1).ToString();
        _characters.Add(character);
        return character;
    }

    /// <summary>
    /// Get a human by ID (async for compatibility with GraphQL.NET patterns).
    /// </summary>
    public Task<Human?> GetHumanByIdAsync(string id)
    {
        var human = _characters.FirstOrDefault(c => c.Id == id && c is Human) as Human;
        return Task.FromResult(human);
    }

    /// <summary>
    /// Get a human by ID (sync version).
    /// </summary>
    public Human? GetHumanById(string id)
    {
        return _characters.FirstOrDefault(c => c.Id == id && c is Human) as Human;
    }

    /// <summary>
    /// Get all humans.
    /// </summary>
    public IEnumerable<Human> GetHumans()
    {
        return _characters.Where(c => c is Human).Cast<Human>().ToList();
    }

    /// <summary>
    /// Get a droid by ID (async for compatibility with GraphQL.NET patterns).
    /// </summary>
    public Task<Droid?> GetDroidByIdAsync(string id)
    {
        var droid = _characters.FirstOrDefault(c => c.Id == id && c is Droid) as Droid;
        return Task.FromResult(droid);
    }

    /// <summary>
    /// Get a droid by ID (sync version).
    /// </summary>
    public Droid? GetDroidById(string id)
    {
        return _characters.FirstOrDefault(c => c.Id == id && c is Droid) as Droid;
    }

    /// <summary>
    /// Get all droids.
    /// </summary>
    public IEnumerable<Droid> GetDroids()
    {
        return _characters.Where(c => c is Droid).Cast<Droid>().ToList();
    }

    /// <summary>
    /// Get a character by ID (async).
    /// </summary>
    public Task<Character?> GetCharacterByIdAsync(string id)
    {
        var character = _characters.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(character);
    }

    /// <summary>
    /// Get a character by ID (sync).
    /// </summary>
    public Character? GetCharacterById(string id)
    {
        return _characters.FirstOrDefault(c => c.Id == id);
    }

    /// <summary>
    /// Get all characters.
    /// </summary>
    public IEnumerable<Character> GetCharacters()
    {
        return _characters.ToList();
    }
}
