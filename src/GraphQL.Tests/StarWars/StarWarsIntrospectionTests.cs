namespace GraphQL.Tests.StarWars
{
    public class StarWarsIntrospectionTests : QueryTestBase<StarWarsSchema>
    {
        [Test]
        public void provides_typename()
        {
            var query = "{ hero { __typename name } }";

            var expected = "{ hero: { __typename: 'Droid', name: 'R2-D2' } }";

            AssertQuerySuccess(query, expected);
        }

        [Test]
        public void allows_query_schema_for_an_object_kind()
        {
            var query = @"
                query IntrospectionDroidKindQuery {
                  __type(name: 'Droid') {
                    name
                    kind
                  }
                }
            ";

            var expected = @"{
              __type: {
                name: 'Droid',
                kind: 'OBJECT'
              }
            }";

            AssertQuerySuccess(query, expected);
        }
    }
}
