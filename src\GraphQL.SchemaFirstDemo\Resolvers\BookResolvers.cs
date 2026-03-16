using GraphQL.SchemaFirstDemo.Models;
using GraphQL.Types;

namespace GraphQL.SchemaFirstDemo;

/// <summary>
/// Resolvers for fields on the <c>Book</c> type.
/// <para>
/// For simple property-to-field mappings (same name, no additional logic) you do
/// not need an explicit resolver class – graphql-dotnet resolves them automatically
/// via reflection. This class is included to show the pattern and to demonstrate
/// how a resolver receives the parent/source object as its first parameter.
/// </para>
/// </summary>
[GraphQLMetadata("Book")]
public class BookResolvers
{
    [GraphQLMetadata("id")]
    public string GetId(Book book) => book.Id;

    [GraphQLMetadata("title")]
    public string GetTitle(Book book) => book.Title;

    [GraphQLMetadata("author")]
    public string GetAuthor(Book book) => book.Author;

    [GraphQLMetadata("genre")]
    public Genre GetGenre(Book book) => book.Genre;

    [GraphQLMetadata("publishedYear")]
    public int? GetPublishedYear(Book book) => book.PublishedYear;
}
