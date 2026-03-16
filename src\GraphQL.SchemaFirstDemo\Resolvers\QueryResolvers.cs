using GraphQL.SchemaFirstDemo.Models;
using GraphQL.Types;

namespace GraphQL.SchemaFirstDemo;

/// <summary>
/// Resolvers for fields on the root <c>Query</c> type.
/// The class name is not significant; what matters is the <see cref="GraphQLMetadata"/>
/// attribute that maps it to the SDL type name "Query".
/// </summary>
[GraphQLMetadata("Query")]
public class QueryResolvers
{
    private readonly IBookRepository _bookRepository;

    public QueryResolvers(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    /// <summary>Returns all books.</summary>
    [GraphQLMetadata("books")]
    public IEnumerable<Book> GetBooks() => _bookRepository.GetAll();

    /// <summary>Returns a single book by its ID, or <see langword="null"/> if not found.</summary>
    [GraphQLMetadata("book")]
    public Book? GetBook(string id) => _bookRepository.GetById(id);
}
