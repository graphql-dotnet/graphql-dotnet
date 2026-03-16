using GraphQL.SchemaFirstDemo.Models;
using GraphQL.Types;

namespace GraphQL.SchemaFirstDemo;

/// <summary>
/// Field resolvers for the <c>Book</c> type.
/// <para>
/// For simple property-to-field mappings (same name, no extra logic) graphql-dotnet
/// resolves them automatically via reflection, so an explicit resolver class is not
/// strictly necessary. This class is included to illustrate the pattern and to show
/// how resolver methods receive the parent/source object as their first parameter.
/// </para>
/// </summary>
[GraphQLMetadata("Book")]
public sealed class BookResolvers
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
