using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.SchemaFirstDemo;

/// <summary>
/// Demonstrates the Schema-First approach: the API surface is defined entirely
/// in SDL and resolver classes are wired up via <see cref="SchemaBuilder"/>.
/// </summary>
public class BookSchema : Schema
{
    /// <summary>
    /// The GraphQL Schema Definition Language (SDL) that describes this API.
    /// In a real application you would typically load this from an embedded resource
    /// or a <c>.graphql</c> file so that it can be shared with clients/tooling.
    /// </summary>
    public const string Sdl = """
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
        """;

    public BookSchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        var builder = new SchemaBuilder();
        builder.Types.Include<QueryResolvers>(serviceProvider);
        builder.Types.Include<MutationResolvers>(serviceProvider);
        builder.Types.Include<BookResolvers>(serviceProvider);

        var builtSchema = builder.Build(Sdl);

        Query    = builtSchema.Query;
        Mutation = builtSchema.Mutation;
    }
}
