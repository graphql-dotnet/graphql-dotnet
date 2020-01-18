using System.Threading.Tasks;
using GraphQL.Utilities.Federation;
using Xunit;
using Newtonsoft.Json.Linq;

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

            var expected = $@"{{ '_service': {{ 'sdl' : '{sdl}' }}}}";

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

            var variables = "{ '_representations': [{ '__typename': 'User', 'id': '123' }] }";
            var expected = @"{ '_entities': [{ '__typename': 'User', 'id' : '123', 'username': 'Quinn' }] }";

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

            var data = JObject.FromObject(Executer.ExecuteAsync(_ =>
            {
                _.Schema = Builder.Build(definitions);
                _.Query = query;
            }).Result.Data).SelectToken("$.__schema.types[?(@.name == '_Entity')].possibleTypes..name");
            
            Assert.Equal("User", data.ToString());
        }
    }
}
