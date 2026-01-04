namespace GraphQL.StarWars.SchemaFirst.Models;

public class Human : IStarWarsCharacter
{
    public string Id { get; set; } = null!;
    public string? Name { get; set; }
    public List<string>? Friends { get; set; }
    public Episodes[]? AppearsIn { get; set; }
    public string? Cursor { get; set; }
    public string? HomePlanet { get; set; }
}
