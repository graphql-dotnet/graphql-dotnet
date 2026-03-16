using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.SchemaFirstDemo;

/// <summary>
/// Demonstrates the Schema-First (SDL-First) approach in GraphQL.NET.
/// <para>
/// The API surface is defined entirely in the GraphQL Schema Definition Language
/// (<see cref="TypeDefinitions"/>), and .NET resolver classes are registered via
/// <see cref="SchemaBuilder"/> so that graphql-dotnet can dispatch field resolution
/// to them at runtime.
/// </para>
/// </summary>
public sealed class BookSchema : Schema
{
    /// <summary>
    /// The GraphQL Schema Definition Language (SDL) that describes this API.
    /// <para>
    /// In a production application you would typically keep this in an embedded
    /// <c>.graphql</c> resource file so that the contract can be shared with
    /// clients and tooling independently of the C# implementation.
    /// </para>
    /// </summary>
    public const string TypeDefinitions = @"
        type Query {
            """"""Returns all books in the library.""""""
            books: [Book!]!

            """"""Returns a single book by its ID, or null if not found.""""""
            book(id: ID!): Book
        }

        type Mutation {
            """"""Adds a new book to the library.""""""
            addBook(input: AddBookInput!): Book!

            """"""Removes a book by ID. Returns true if the book was found and deleted.""""""
            deleteBook(id: ID!): Boolean!
        }

        type Book {
            id: ID!
            title: String!
            author: String!
            genre: Genre!
            publishedYear: Int
        }

        input AddBookInput {
            title: String!
            author: String!
            genre: Genre!
            publishedYear: Int
        }

        enum Genre {
            FICTION
            NON_FICTION
            SCIENCE
            HISTORY
            BIOGRAPHY
        }
    ";

    public BookSchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        // Build the schema from SDL and wire up resolver classes.
        // SchemaBuilder.Types.Include<T> reads [GraphQLMetadata] attributes on T
        // to map it to a named SDL type and its individual field resolver methods.
        var builder = new SchemaBuilder();
        builder.Types.Include<QueryResolvers>(serviceProvider);
        builder.Types.Include<MutationResolvers>(serviceProvider);
        builder.Types.Include<BookResolvers>(serviceProvider);

        var schema = builder.Build(TypeDefinitions);

        Query    = schema.Query;
        Mutation = schema.Mutation;
    }
}
