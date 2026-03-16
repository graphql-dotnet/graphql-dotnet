using GraphQL.SchemaFirstDemo.Models;
using GraphQL.Types;

namespace GraphQL.SchemaFirstDemo;

/// <summary>
/// Resolvers for fields on the root <c>Mutation</c> type.
/// </summary>
[GraphQLMetadata("Mutation")]
public class MutationResolvers
{
    private readonly IBookRepository _bookRepository;

    public MutationResolvers(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    /// <summary>Adds a new book and returns it.</summary>
    [GraphQLMetadata("addBook")]
    public Book AddBook(AddBookInput input) => _bookRepository.Add(input);

    /// <summary>Deletes a book by ID. Returns <see langword="true"/> if the book was found and removed.</summary>
    [GraphQLMetadata("deleteBook")]
    public bool DeleteBook(string id) => _bookRepository.Delete(id);
}
