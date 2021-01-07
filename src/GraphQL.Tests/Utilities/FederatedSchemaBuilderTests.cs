using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GraphQL.DataLoader;
using GraphQL.Utilities.Federation;
using Xunit;

namespace GraphQL.Tests.Utilities
{
    public class FederatedSchemaBuilderTests : FederatedSchemaBuilderTestBase
    {
        public class User
        {
            public string Id { get; set; }
            public string Username { get; set; }
        }

        [Fact]
        public void returns_sdl()
        {
            var definitions = @"
                extend type Query {
                    me: User
                }

                type User @key(fields: ""id"") {
                    id: ID! @external
                    username: String!
                }
            ";

            var query = "{ _service { sdl } }";

            var sdl = @"extend type Query {
  me: User
}

type User @key(fields: ""id"") {
  id: ID! @external
  username: String!
}
";
            var expected = @"{ ""_service"": { ""sdl"" : """ + JsonEncodedText.Encode(sdl) + @""" } }";

            AssertQuery(_ =>
            {
                _.Definitions = definitions;
                _.Query = query;
                _.ExpectedResult = expected;
            });
        }

        [Fact]
        public void entity_query()
        {
            var definitions = @"
                extend type Query {
                    me: User
                }

                type User @key(fields: ""id"") {
                    id: ID!
                    username: String!
                }
            ";

            Builder.Types.For("User").ResolveReferenceAsync(ctx => Task.FromResult(new User { Id = "123", Username = "Quinn" }));

            var query = @"
                query ($_representations: [_Any!]!) {
                    _entities(representations: $_representations) {
                        ... on User {
                            id
                            username
                        }
                    }
                }";

            var variables = @"{ ""_representations"": [{ ""__typename"": ""User"", ""id"": ""123"" }] }";
            var expected = @"{ ""_entities"": [{ ""__typename"": ""User"", ""id"" : ""123"", ""username"": ""Quinn"" }] }";

            AssertQuery(_ =>
            {
                _.Definitions = definitions;
                _.Query = query;
                _.Variables = variables;
                _.ExpectedResult = expected;
            });
        }

        [Theory]
        [InlineData("...on User { id }", false)]
        [InlineData("__typename ...on User { id }", false)]
        [InlineData("...on User { __typename id }", false)]
        [InlineData("...on User { ...TypeAndId }", true)]
        public void result_includes_typename(string selectionSet, bool includeFragment)
        {
            var definitions = @"
                extend type Query {
                    me: User
                }

                type User @key(fields: ""id"") {
                    id: ID!
                    username: String!
                }
            ";

            Builder.Types.For("User").ResolveReferenceAsync(ctx => Task.FromResult(new User { Id = "123", Username = "Quinn" }));

            var query = @$"
                query ($_representations: [_Any!]!) {{
                    _entities(representations: $_representations) {{
                        {selectionSet}
                    }}
                }}";
            if (includeFragment)
            {
                query += @"
                fragment TypeAndId on User {
                    __typename
                    id
                }
                ";
            }

            var variables = @"{ ""_representations"": [{ ""__typename"": ""User"", ""id"": ""123"" }] }";
            var expected = @"{ ""_entities"": [{ ""__typename"": ""User"", ""id"" : ""123""}] }";

            AssertQuery(_ =>
            {
                _.Definitions = definitions;
                _.Query = query;
                _.Variables = variables;
                _.ExpectedResult = expected;
            });
        }

        [Fact]
        public void input_types_and_types_without_key_directive_are_not_added_to_entities_union()
        {
            var definitions = @"
                input UserInput {
                    limit: Int!
                    offset: Int
                }

                type Comment {
                    id: ID!
                }

                type User @key(fields: ""id"") {
                    id: ID! @external
                }
            ";

            var query = "{ __schema { types { name kind possibleTypes { name } } } }";

            var executionResult = Executer.ExecuteAsync(_ =>
            {
                _.Schema = Builder.Build(definitions);
                _.Query = query;
            }).GetAwaiter().GetResult();

            var data = (Dictionary<string, object>)executionResult.Data;
            var schema = (Dictionary<string, object>)data["__schema"];
            var types = (List<object>)schema["types"];
            var entityType = (Dictionary<string, object>)types.Single(t => (string)((Dictionary<string, object>)t)["name"] == "_Entity");
            var possibleTypes = (List<object>)entityType["possibleTypes"];
            var possibleType = (Dictionary<string, object>)possibleTypes[0];
            var name = (string)possibleType["name"];

            Assert.Equal("User", name);
        }

        [Fact]
        public void resolve_reference_is_not_trying_to_await_for_each_field_individialy_and_plays_well_with_dataloader_issue_1565()
        {
            var definitions = @"
                extend type Query {
                    user(id: ID!): User
                }
                type User @key(fields: ""id"") {
                    id: ID!
                    username: String!
                }
            ";

            var users = new List<User> {
                new User { Id = "1", Username = "One" },
                new User { Id = "2", Username = "Two" },
            };


            var accessor = new DataLoaderContextAccessor
            {
                Context = new DataLoaderContext()
            };
            var listener = new DataLoaderDocumentListener(accessor);

            Builder.Types.For("User").ResolveReferenceAsync(ctx =>
            {
                var id = ctx.Arguments["id"].ToString();
                // return Task.FromResult(users.FirstOrDefault(user => user.Id == id));
                var loader = accessor.Context.GetOrAddBatchLoader<string, User>("GetAccountByIdAsync", ids =>
                {
                    var results = users.Where(user => ids.Contains(user.Id));
                    return Task.FromResult((IDictionary<string, User>)results.ToDictionary(c => c.Id));
                });
                return Task.FromResult(loader.LoadAsync(id));
            });

            var query = @"
                {
                    _entities(representations: [{__typename: ""User"", id: ""1"" }, {__typename: ""User"", id: ""2"" }]) {
                        ... on User {
                            id
                            username
                        }
                    }
                }";

            var expected = @"{ ""_entities"": [{ ""__typename"": ""User"", ""id"" : ""1"", ""username"": ""One"" }, { ""__typename"": ""User"", ""id"" : ""2"", ""username"": ""Two"" }] }";

            AssertQuery(_ =>
            {
                _.Definitions = definitions;
                _.Query = query;
                _.ExpectedResult = expected;
                _.Listeners.Add(listener);
            });
        }
    }
}
