using GraphQL.SchemaFirstDemo.Models;
using GraphQL.Types;

namespace GraphQL.SchemaFirstDemo;

/// <summary>
/// Field resolvers for the root <c>Mutation</c> type.
/// </summary>
[GraphQLMetadata("Mutation")]
public sealed class MutationResolvers
{
    private readonly IBookRepository _bookRepository;

    public MutationResolvers(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    /// <summary>Resolves <c>Mutation.addBook(input:)</c>.</summary>
    [GraphQLMetadata("addBook")]
    public Book AddBook(AddBookInput input) => _bookRepository.Add(input);

    /// <summary>Resolves <c>Mutation.deleteBook(id:)</c>.</summary>
    [GraphQLMetadata("deleteBook")]
    public bool DeleteBook(string id) => _bookRepository.Delete(id);
}
