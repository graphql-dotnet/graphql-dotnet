using System.Text.Json.Serialization;

namespace GraphQL.SchemaFirst.Sample;

/// <summary>
/// Human type - represents a human character.
/// Maps to the SDL type definition: type Human implements Character
/// </summary>
public class Human : Character
{
    public string Id { get; set; } = "";
    public string? Name { get; set; }
    public List<Episode>? AppearsIn { get; set; }
    public List<string>? Friends { get; set; }
    
    /// <summary>
    /// Home planet of the human.
    /// Maps to SDL field: homePlanet: String
    /// </summary>
    public string? HomePlanet { get; set; }
    
    /// <summary>
    /// Cursor for pagination.
    /// </summary>
    [JsonIgnore]
    public string? Cursor { get; set; }
}
