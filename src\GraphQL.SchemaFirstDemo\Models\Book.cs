namespace GraphQL.SchemaFirstDemo.Models;

/// <summary>Domain model returned by the Books API.</summary>
public class Book
{
    public string Id { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Author { get; set; } = default!;
    public Genre Genre { get; set; }
    public int? PublishedYear { get; set; }
}
