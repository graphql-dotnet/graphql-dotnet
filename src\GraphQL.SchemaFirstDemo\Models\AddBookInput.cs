namespace GraphQL.SchemaFirstDemo.Models;

public class AddBookInput
{
    public string Title { get; set; } = default!;
    public string Author { get; set; } = default!;
    public Genre Genre { get; set; }
    public int? PublishedYear { get; set; }
}
