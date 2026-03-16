using GraphQL.SchemaFirstDemo.Models;

namespace GraphQL.SchemaFirstDemo;

/// <summary>
/// Simple in-memory repository used to back the Schema-First sample.
/// </summary>
public class BookRepository : IBookRepository
{
    private readonly List<Book> _books = new()
    {
        new Book { Id = "1", Title = "The Hitchhiker's Guide to the Galaxy", Author = "Douglas Adams",      Genre = Genre.Fiction,    PublishedYear = 1979 },
        new Book { Id = "2", Title = "A Brief History of Time",               Author = "Stephen Hawking",   Genre = Genre.Science,    PublishedYear = 1988 },
        new Book { Id = "3", Title = "Sapiens",                               Author = "Yuval Noah Harari", Genre = Genre.History,    PublishedYear = 2011 },
        new Book { Id = "4", Title = "Clean Code",                            Author = "Robert C. Martin",  Genre = Genre.NonFiction, PublishedYear = 2008 },
    };

    private int _nextId = 5;

    public IEnumerable<Book> GetAll() => _books.AsReadOnly();

    public Book? GetById(string id) =>
        _books.FirstOrDefault(b => b.Id == id);

    public Book Add(AddBookInput input)
    {
        var book = new Book
        {
            Id = (_nextId++).ToString(),
            Title = input.Title,
            Author = input.Author,
            Genre = input.Genre,
            PublishedYear = input.PublishedYear,
        };
        _books.Add(book);
        return book;
    }

    public bool Delete(string id)
    {
        var book = _books.FirstOrDefault(b => b.Id == id);
        if (book == null) return false;
        _books.Remove(book);
        return true;
    }
}
