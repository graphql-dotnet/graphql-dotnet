using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.SchemaFirstDemo;

/// <summary>
/// Schema-First schema definition for a simple Books API.
/// The SDL (Schema Definition Language) string defines the schema structure,
/// and <see cref="BookSchemaConfigurator"/> maps types/fields to .NET resolver logic.
/// </summary>
public class BookSchema : Schema
{
    // The SDL defines the shape of your API.
    private const string Sdl = @"
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
        // Build the schema from SDL and wire up resolvers via the configurator.
        var schema = For(Sdl, configurator =>
        {
            configurator.Types.Include<QueryType>("Query");
            configurator.Types.Include<MutationType>("Mutation");
            configurator.Types.Include<BookType>("Book");
        });

        Query = schema.Query;
        Mutation = schema.Mutation;
    }
}
