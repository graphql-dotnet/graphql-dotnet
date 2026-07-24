using System.Reflection;
using GraphQL;
using GraphQL.SystemTextJson;
using GraphQL.Types;

var schemaText = LoadResource("Schema.gql");

var schema = Schema.For(schemaText, builder =>
{
    builder.Types.Include<Query>();
    builder.Types.Include<Mutation>();
});

var listResult = await schema.ExecuteAsync(options =>
{
    options.Query = """
    {
      books {
        id
        title
        author { name }
        tags
      }
    }
    """;
    options.ThrowOnUnhandledException = true;
}).ConfigureAwait(false);

Console.WriteLine("Query result:");
Console.WriteLine(listResult);
Console.WriteLine();

var mutationResult = await schema.ExecuteAsync(options =>
{
    options.Query = """
    mutation AddBook($book: AddBookInput!) {
      addBook(input: $book) {
        id
        title
        pages
        author { name }
        tags
      }
    }
    """;
    options.Variables = new Inputs(new Dictionary<string, object?>
    {
        ["book"] = new Dictionary<string, object?>
        {
            ["title"] = "Schema-first GraphQL.NET",
            ["authorId"] = "2",
            ["pages"] = 248,
            ["tags"] = new[] { "schema-first", "sample" },
        },
    });
    options.ThrowOnUnhandledException = true;
}).ConfigureAwait(false);

Console.WriteLine("Mutation result:");
Console.WriteLine(mutationResult);

return listResult.Contains("\"books\"") && mutationResult.Contains("Schema-first GraphQL.NET") ? 0 : 1;

static string LoadResource(string resourceName)
{
    var assembly = Assembly.GetExecutingAssembly();
    var fullName = assembly.GetManifestResourceNames().SingleOrDefault(name => name.EndsWith($".{resourceName}", StringComparison.Ordinal))
        ?? throw new InvalidOperationException($"Could not find embedded resource ending with '{resourceName}'.");
    using var stream = assembly.GetManifestResourceStream(fullName)
        ?? throw new InvalidOperationException($"Could not read embedded resource '{fullName}'.");
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
}

public class Query
{
    [GraphQLMetadata("authors")]
    public IEnumerable<Author> GetAuthors() => SampleData.Authors;

    [GraphQLMetadata("books")]
    public IEnumerable<Book> GetBooks() => SampleData.Books;

    [GraphQLMetadata("book")]
    public Book? GetBook(string id) => SampleData.Books.SingleOrDefault(book => book.Id == id);
}

public class Mutation
{
    [GraphQLMetadata("addBook")]
    public Book AddBook(AddBookInput input)
    {
        var author = SampleData.Authors.SingleOrDefault(author => author.Id == input.AuthorId)
            ?? throw new ExecutionError($"Author '{input.AuthorId}' was not found.");

        var book = new Book
        {
            Id = (SampleData.Books.Count + 1).ToString(),
            Title = input.Title,
            Author = author,
            Pages = input.Pages,
            Tags = input.Tags ?? [],
        };
        SampleData.Books.Add(book);
        return book;
    }
}

public sealed class AddBookInput
{
    public string Title { get; set; } = null!;
    public string AuthorId { get; set; } = null!;
    public int Pages { get; set; }
    public string[]? Tags { get; set; }
}

public sealed class Author
{
    public string Id { get; init; } = null!;
    public string Name { get; init; } = null!;
}

public sealed class Book
{
    public string Id { get; init; } = null!;
    public string Title { get; init; } = null!;
    public Author Author { get; init; } = null!;
    public int Pages { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
}

internal static class SampleData
{
    public static readonly IReadOnlyList<Author> Authors =
    [
        new() { Id = "1", Name = "Octavia Butler" },
        new() { Id = "2", Name = "Ursula K. Le Guin" },
    ];

    public static readonly List<Book> Books =
    [
        new()
        {
            Id = "1",
            Title = "Parable of the Sower",
            Author = Authors[0],
            Pages = 345,
            Tags = ["dystopian", "classic"],
        },
        new()
        {
            Id = "2",
            Title = "The Left Hand of Darkness",
            Author = Authors[1],
            Pages = 304,
            Tags = ["science-fiction", "classic"],
        },
    ];
}
