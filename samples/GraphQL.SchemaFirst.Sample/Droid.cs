using System.Text.Json.Serialization;

namespace GraphQL.SchemaFirst.Sample;

/// <summary>
/// Droid type - represents a droid character.
/// Maps to the SDL type definition: type Droid implements Character
/// </summary>
public class Droid : Character
{
    public string Id { get; set; } = "";
    public string? Name { get; set; }
    public List<Episode>? AppearsIn { get; set; }
    public List<string>? Friends { get; set; }
    
    /// <summary>
    /// Primary function of the droid.
    /// Maps to SDL field: primaryFunction: String
    /// </summary>
    public string? PrimaryFunction { get; set; }
    
    /// <summary>
    /// Cursor for pagination.
    /// </summary>
    [JsonIgnore]
    public string? Cursor { get; set; }
}
