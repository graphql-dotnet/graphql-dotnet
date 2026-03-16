using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.SchemaFirstDemo;

/// <summary>
/// Demonstrates the Schema-First approach: the full API surface is declared in
/// GraphQL SDL and .NET resolver classes are registered via <see cref="SchemaBuilder"/>.
/// </summary>
public class BookSchema : Schema
{
    /// <summary>
    /// The GraphQL Schema Definition Language that describes this API.
    /// In a real project you would typically keep this in an embedded <c>.graphql</c>
    /// resource file so it can be shared with clients and tooling.
    /// </summary>
    public const string TypeDefinitions = @"
        type Query {
            books: [Book!]!
            book(id: ID!): Book
        }

        type Mutation {
            addBook(input: AddBookInput!): Book!
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
        // SchemaBuilder parses the SDL and returns a ready-to-use Schema whose
        // types/fields have resolvers populated from the registered resolver classes.
        var builder = new SchemaBuilder();

        // Register resolver classes. SchemaBuilder resolves them from the DI container
        // so they can declare constructor dependencies normally.
        builder.Types.Include<QueryResolvers>(serviceProvider);
        builder.Types.Include<MutationResolvers>(serviceProvider);
        builder.Types.Include<BookResolvers>(serviceProvider);

        var schema = builder.Build(TypeDefinitions);

        Query    = schema.Query;
        Mutation = schema.Mutation;
    }
}
