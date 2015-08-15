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
        public void allows_querying_schema_for_an_object_kind()
        {
            var query = @"
                query IntrospectionDroidKindQuery {
                  __type(name: 'Droid') {
                    name,
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

        [Test]
        public void allows_querying_schema_for_an_interface_kind()
        {
            var query = @"
            query IntrospectionCharacterKindQuery {
              __type(name: ""Character"") {
                name
                kind
              }
            }
            ";

            var expected = @"{
              __type: {
                name: 'Character',
                kind: 'INTERFACE'
              }
            }";

            AssertQuerySuccess(query, expected);
        }

        [Test]
        public void allows_querying_schema_for_possibleTypes_of_an_interface()
        {
            var query = @"
            query IntrospectionCharacterKindQuery {
              __type(name: ""Character"") {
                name
                kind
                  possibleTypes {
                    name,
                    kind
                  }
              }
            }
            ";

            var expected = @"{
              __type: {
                name: 'Character',
                kind: 'INTERFACE',
                possibleTypes: [
                  { name: 'Human', kind: 'OBJECT' },
                  { name: 'Droid', kind: 'OBJECT' },
                ]
              }
            }";

            AssertQuerySuccess(query, expected);
        }

        [Test]
        public void allows_querying_the_schema_for_object_fields()
        {
            var query = @"
            query IntrospectionDroidFieldsQuery {
              __type(name: ""Droid"") {
                name
                fields {
                    name
                    type {
                        name
                        kind
                    }
                }
              }
            }
            ";

            var expected = @"{
                __type: {
                  name: 'Droid',
                  fields: [
                    {
                      name: 'id',
                      type: {
                        name: null,
                        kind: 'NON_NULL'
                      }
                    },
                    {
                      name: 'name',
                      type: {
                        name: 'String',
                        kind: 'SCALAR'
                      }
                    },
                    {
                      name: 'friends',
                      type: {
                        name: null,
                        kind: 'LIST'
                      }
                    },
                    {
                      name: 'appearsIn',
                      type: {
                        name: null,
                        kind: 'LIST'
                      }
                    },
                    {
                      name: 'primaryFunction',
                      type: {
                        name: 'String',
                        kind: 'SCALAR'
                      }
                    }
                  ]
                }
            }";

            AssertQuerySuccess(query, expected);
        }

        [Test]
        public void allows_querying_the_schema_for_documentation()
        {
            var query = @"
            query IntrospectionDroidDescriptionQuery {
              __type(name: ""Droid"") {
                name
                description
              }
            }
            ";
            var expected = @"{
            '__type': {
              'name': 'Droid',
              'description': 'A mechanical creature in the Star Wars universe.'
            }
            }";

            AssertQuerySuccess(query, expected);
        }

        [Test]
        public void allows_querying_the_schema()
        {
            var query = @"
            query SchemaIntrospectionQuery {
              __schema {
                queryType { name, kind }
                mutationType { name }
                directives {
                  name
                  description
                  onOperation
                  onFragment
                  onField
                }
              }
            }
            ";
            var expected = @"{
            '__schema': {
              'queryType': { 'name':'Query', 'kind': 'OBJECT'},
              'mutationType': null,
              'directives': [],
            }
            }";

            AssertQuerySuccess(query, expected);
        }
    }
}
