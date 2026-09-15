namespace GraphQL.SchemaFirst.Sample;

/// <summary>
/// Character interface - represents a character in the Star Wars universe.
/// Maps to the SDL interface definition.
/// </summary>
public interface Character
{
    string Id { get; set; }
    string? Name { get; set; }
    List<Episode>? AppearsIn { get; set; }
    List<string>? Friends { get; set; }
}
