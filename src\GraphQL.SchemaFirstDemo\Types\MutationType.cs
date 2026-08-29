using GraphQL.SchemaFirstDemo.Models;
using GraphQL.Types;

namespace GraphQL.SchemaFirstDemo;

/// <summary>
/// Resolver class for the <c>Mutation</c> type defined in the SDL.
/// </summary>
[GraphQLMetadata("Mutation")]
public class MutationType
{
    private readonly IBookRepository _bookRepository;

    public MutationType(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    [GraphQLMetadata("addBook")]
    public Book AddBook(AddBookInput input) => _bookRepository.Add(input);

    [GraphQLMetadata("deleteBook")]
    public bool DeleteBook(string id) => _bookRepository.Delete(id);
}
