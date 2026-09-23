using GraphQL.SchemaFirstDemo.Models;
using GraphQL.Types;

namespace GraphQL.SchemaFirstDemo;

/// <summary>
/// Resolver class for the <c>Book</c> type defined in the SDL.
/// When all fields are simple property mappings you don't strictly need a dedicated
/// resolver class – graphql-dotnet will resolve them automatically via reflection.
/// This class is here to show the pattern and to demonstrate a computed field.
/// </summary>
[GraphQLMetadata("Book")]
public class BookType
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
