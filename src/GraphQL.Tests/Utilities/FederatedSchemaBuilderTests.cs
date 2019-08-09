using System.Threading.Tasks;
using GraphQL.Utilities.Federation;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Utilities
{
    public class FederatedSchemaBuilderTests
    {
        public class User
        {
            public string Id { get; set; }
            public string Username { get; set; }
            public string Other { get; set; }
        }

        [Fact]
        public void something()
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

            var schema = FederatedSchema.For(definitions);

            var result = schema.Execute(_ => {
                _.Query = "{ _service { sdl } }";
            });

            result.ShouldNotBeNull();
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

            var schema = FederatedSchema.For(definitions, _ =>
            {
                 _.Types.For("User").ResolveReferenceAsync(ctx =>
                {
                    return Task.FromResult(new User { Id = "123", Username = "Quinn", Other = "Hrm" });
                });
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

            var variables = "{ '_representations': [ { '__typename': 'User', 'id': '123' } ] }";

            var result = schema.Execute(_ => {
                _.Query = query;
                _.Inputs = variables.ToInputs();
                _.ExposeExceptions = true;
            });

            result.ShouldNotBeNull();
        }
    }
}
