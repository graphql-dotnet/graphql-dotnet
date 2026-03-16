namespace GraphQL.SchemaFirstDemo.Models;

/// <summary>Input model for the <c>addBook</c> mutation.</summary>
public class AddBookInput
{
    public string Title { get; set; } = default!;
    public string Author { get; set; } = default!;
    public Genre Genre { get; set; }
    public int? PublishedYear { get; set; }
}
