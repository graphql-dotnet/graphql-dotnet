namespace GraphQL.StarWars.SchemaFirst.Models;

public interface IStarWarsCharacter
{
    string Id { get; set; }
    string? Name { get; set; }
    List<string>? Friends { get; set; }
    Episodes[]? AppearsIn { get; set; }
    string? Cursor { get; set; }
}
