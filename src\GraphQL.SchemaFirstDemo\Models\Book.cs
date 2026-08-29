namespace GraphQL.SchemaFirstDemo.Models;

/// <summary>Domain model for a book returned by the API.</summary>
public sealed class Book
{
    public string Id { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Author { get; set; } = default!;
    public Genre Genre { get; set; }
    public int? PublishedYear { get; set; }
}
