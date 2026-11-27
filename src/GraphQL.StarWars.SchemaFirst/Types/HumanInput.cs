namespace GraphQL.StarWars.SchemaFirst.Types;

public class HumanInput
{
    public string Name { get; set; } = string.Empty;

    public string? HomePlanet { get; set; }
}
