using GraphQL.SchemaFirstDemo.Models;

namespace GraphQL.SchemaFirstDemo;

/// <summary>
/// Simple in-memory implementation of <see cref="IBookRepository"/>.
/// Pre-seeded with a handful of books so that queries return data immediately.
/// </summary>
public sealed class BookRepository : IBookRepository
{
    private readonly List<Book> _books =
    [
        new() { Id = "1", Title = "The Hitchhiker's Guide to the Galaxy", Author = "Douglas Adams",       Genre = Genre.Fiction,    PublishedYear = 1979 },
        new() { Id = "2", Title = "A Brief History of Time",              Author = "Stephen Hawking",    Genre = Genre.Science,    PublishedYear = 1988 },
        new() { Id = "3", Title = "Sapiens",                              Author = "Yuval Noah Harari",  Genre = Genre.History,    PublishedYear = 2011 },
        new() { Id = "4", Title = "Clean Code",                           Author = "Robert C. Martin",   Genre = Genre.NonFiction, PublishedYear = 2008 },
    ];

    private int _nextId = 5;

    /// <inheritdoc/>
    public IEnumerable<Book> GetAll() => _books.AsReadOnly();

    /// <inheritdoc/>
    public Book? GetById(string id) =>
        _books.FirstOrDefault(b => b.Id == id);

    /// <inheritdoc/>
    public Book Add(AddBookInput input)
    {
        var book = new Book
        {
            Id           = (_nextId++).ToString(),
            Title        = input.Title,
            Author       = input.Author,
            Genre        = input.Genre,
            PublishedYear = input.PublishedYear,
        };
        _books.Add(book);
        return book;
    }

    /// <inheritdoc/>
    public bool Delete(string id)
    {
        var book = _books.FirstOrDefault(b => b.Id == id);
        if (book is null) return false;
        _books.Remove(book);
        return true;
    }
}
