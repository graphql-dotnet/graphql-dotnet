using GraphQL.SchemaFirstDemo.Models;
using GraphQL.Types;

namespace GraphQL.SchemaFirstDemo;

/// <summary>
/// Resolver class for the <c>Query</c> type defined in the SDL.
/// Each method corresponds to a field on the Query type.
/// Method names must match field names (case-insensitive) or be decorated with <see cref="GraphQLMetadata"/>.
/// </summary>
[GraphQLMetadata("Query")]
public class QueryType
{
    private readonly IBookRepository _bookRepository;

    public QueryType(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    [GraphQLMetadata("books")]
    public IEnumerable<Book> GetBooks() => _bookRepository.GetAll();

    [GraphQLMetadata("book")]
    public Book? GetBook(string id) => _bookRepository.GetById(id);
}
