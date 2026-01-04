namespace GraphQL.StarWars.SchemaFirst.Models;

public class HumanInput
{
    public string Name { get; set; } = null!;
    public string? HomePlanet { get; set; }
}
