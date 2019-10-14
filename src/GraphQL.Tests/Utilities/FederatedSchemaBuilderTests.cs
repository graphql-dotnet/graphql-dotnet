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

            Builder.Types.For("User").ResolveReferenceAsync(ctx =>
            {
                return Task.FromResult(new User { Id = "123", Username = "Quinn" });
            });

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
    }
}
