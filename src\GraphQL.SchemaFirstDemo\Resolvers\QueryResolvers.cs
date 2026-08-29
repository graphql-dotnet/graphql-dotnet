using GraphQL.SchemaFirstDemo.Models;
using GraphQL.Types;

namespace GraphQL.SchemaFirstDemo;

/// <summary>
/// Field resolvers for the root <c>Query</c> type.
/// <para>
/// The <see cref="GraphQLMetadata"/> attribute on the class maps it to the SDL type
/// named <c>Query</c>. Each method decorated with <see cref="GraphQLMetadata"/> maps
/// to an SDL field of the same (logical) name.
/// </para>
/// </summary>
[GraphQLMetadata("Query")]
public sealed class QueryResolvers
{
    private readonly IBookRepository _bookRepository;

    public QueryResolvers(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    /// <summary>Resolves <c>Query.books</c>.</summary>
    [GraphQLMetadata("books")]
    public IEnumerable<Book> GetBooks() => _bookRepository.GetAll();

    /// <summary>Resolves <c>Query.book(id:)</c>.</summary>
    [GraphQLMetadata("book")]
    public Book? GetBook(string id) => _bookRepository.GetById(id);
}
