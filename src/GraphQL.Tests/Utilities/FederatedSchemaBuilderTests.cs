using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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

            var sdl = @"scalar BigInt

scalar Byte

scalar Date

scalar DateTime

scalar DateTimeOffset

scalar Decimal

scalar Guid

scalar Long

scalar Milliseconds

extend type Query {
  me: User
}

scalar SByte

scalar Seconds

scalar Short

scalar UInt

scalar ULong

scalar UShort

scalar Uri

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
    }
}
