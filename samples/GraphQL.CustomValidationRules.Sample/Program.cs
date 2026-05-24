using GraphQL.CustomValidationRules.Sample.Schema;
using GraphQL.CustomValidationRules.Sample.Validation;
using GraphQL.Types;
using GraphQL.Utilities;

var builder = WebApplication.CreateBuilder(args);

// Register data source
builder.Services.AddSingleton<BlogData>();

// Register schema as a factory
builder.Services.AddSingleton<ISchema>(sp =>
{
    var schema = Schema.For("""
        type Post {
            id: ID!
            title: String!
            content: String!
            author: String!
            comments: [Comment!]!
        }

        type Comment {
            id: ID!
            text: String!
            author: String!
        }

        type Query {
            posts: [Post!]!
            post(id: ID!): Post
        }

        type Mutation {
            addPost(title: String!, content: String!, author: String!): Post!
        }
        """, opts =>
    {
        opts.Types.Include<Query>();
        opts.Types.Include<Mutation>();
        opts.ServiceProvider = sp;
    });
    return schema;
});

// Register GraphQL with custom validation rules
builder.Services.AddGraphQL(b => b
    .AddSystemTextJson()
    .AddSchema<ISchema>()
    // Custom rule: limit query depth to prevent deeply nested queries
    .AddValidationRule<MaxDepthValidationRule>()
    // Custom rule: restrict the number of fields in a selection set
    .AddValidationRule<MaxFieldsValidationRule>()
);

var app = builder.Build();

app.UseGraphQL<ISchema>();
app.UseGraphQLGraphiQL("/");

// Ensure schema is initialized
app.Services.GetRequiredService<ISchema>().Initialize();

await app.RunAsync();
