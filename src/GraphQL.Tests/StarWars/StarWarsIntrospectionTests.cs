namespace GraphQL.Tests.StarWars
{
    public class StarWarsIntrospectionTests : StarWarsTestBase
    {
        [Fact]
        public void provides_typename()
        {
            var query = "{ hero { __typename name } }";

            var expected = "{ hero: { __typename: 'Droid', name: 'R2-D2' } }";

            AssertQuerySuccess(query, expected);
        }

        [Fact]
        public void allows_querying_schema_for_an_object_kind()
        {
            var query = @"
                query IntrospectionDroidKindQuery {
                  __type(name: ""Droid"") {
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
        public void allows_querying_the_schema()
        {
            var query = @"
            query SchemaIntrospectionQuery {
              __schema {
                types { name, kind }
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
              'types': [
                {
                  'name': 'String',
                  'kind': 'SCALAR'
                },
                {
                  'name': 'Boolean',
                  'kind': 'SCALAR'
                },
                {
                  'name': 'Float',
                  'kind': 'SCALAR'
                },
                {
                  'name': 'Int',
                  'kind': 'SCALAR'
                },
                {
                  'name': 'ID',
                  'kind': 'SCALAR'
                },
                {
                  'name': 'Date',
                  'kind': 'SCALAR'
                },
                {
                  'name': 'Decimal',
                  'kind': 'SCALAR'
                },
                {
                  'name': '__Schema',
                  'kind': 'OBJECT'
                },
                {
                  'name': '__Type',
                  'kind': 'OBJECT'
                },
                {
                  'name': '__TypeKind',
                  'kind': 'ENUM'
                },
                {
                  'name': '__Field',
                  'kind': 'OBJECT'
                },
                {
                  'name': '__InputValue',
                  'kind': 'OBJECT'
                },
                {
                  'name': '__EnumValue',
                  'kind': 'OBJECT'
                },
                {
                  'name': '__Directive',
                  'kind': 'OBJECT'
                },
                {
                  'name': 'Query',
                  'kind': 'OBJECT'
                },
                {
                  'name': 'Character',
                  'kind': 'INTERFACE'
                },
                {
                  'name': 'Episode',
                  'kind': 'ENUM'
                },
                {
                  'name': 'Human',
                  'kind': 'OBJECT'
                },
                {
                  'name': 'Droid',
                  'kind': 'OBJECT'
                }
              ],
              'queryType': {
                'name': 'Query',
                'kind': 'OBJECT'
              },
              'mutationType': null,
              'directives': [
                {
                  'name': 'include',
                  'description': 'Directs the executor to include this field or fragment only when the \'if\' argument is true.',
                  'onOperation': false,
                  'onFragment': true,
                  'onField': true
                },
                {
                  'name': 'skip',
                  'description': 'Directs the executor to skip this field or fragment when the \'if\' argument is true.',
                  'onOperation': false,
                  'onFragment': true,
                  'onField': true
                }
              ]
            }
            }";

            AssertQuerySuccess(query, expected);
        }

        [Fact]
        public void allows_querying_field_args()
        {
            var query = @"
            query SchemaIntrospectionQuery {
              __schema {
                queryType {
                  fields {
                    name
                    args {
                      name
                      description
                      type {
                        name
                        kind
                        ofType {
                          name
                          kind
                        }
                      }
                      defaultValue
                    }
                  }
                }
              }
            }
            ";
            var expected = @"{
            '__schema': {
              'queryType': {
                'fields': [
                  {
                    'name': 'hero',
                    'args': []
                  },
                  {
                    'name': 'human',
                    'args': [
                      {
                        'name': 'id',
                        'description': 'id of the human',
                        'type': {
                          'name': null,
                          'kind': 'NON_NULL',
                          'ofType': {
                            'name': 'String',
                            'kind': 'SCALAR'
                          }
                        },
                        'defaultValue': null
                      }
                    ]
                  },
                  {
                    'name': 'droid',
                    'args': [
                      {
                        'name': 'id',
                        'description': 'id of the droid',
                        'type': {
                          'name': null,
                          'kind': 'NON_NULL',
                          'ofType': {
                            'name': 'String',
                            'kind': 'SCALAR'
                          }
                        },
                        'defaultValue': null
                      }
                    ]
                  }
                ]
              }
            }
            }";

            AssertQuerySuccess(query, expected);
        }

        //imho this should be broken down into smaller bite sized junks we are specifically testing for. Or test that it runs? Just seems like another thing to maintain.
        //[Fact]
        public void full_schema_query()
        {
            var query = @"
            query SchemaIntrospectionQuery {
              __schema {
                queryType { name, kind }
                types { 
                    kind
                    name
                    description
                    fields {
                        name
                        description
                        type {
                            name
                            kind
                        }
                        isDeprecated
                        deprecationReason
                    }
                }
                mutationType { name }
                directives {
                  name
                  description
                  onOperation
                  onFragment
                  onField
                }
              }
            }";

            var expected = @"{
            '__schema': {
              'queryType': {
                'name': 'Query',
                'kind': 'OBJECT'
              },
              'types': [
                {
                  'kind': 'SCALAR',
                  'name': 'String',
                  'description': null,
                  'fields': null
                },
                {
                  'kind': 'SCALAR',
                  'name': 'Boolean',
                  'description': null,
                  'fields': null
                },
                {
                  'kind': 'SCALAR',
                  'name': 'Float',
                  'description': null,
                  'fields': null
                },
                {
                  'kind': 'SCALAR',
                  'name': 'Int',
                  'description': null,
                  'fields': null
                },
                {
                  'kind': 'SCALAR',
                  'name': 'ID',
                  'description': null,
                  'fields': null
                },
                {
                  'kind': 'SCALAR',
                  'name': 'Date',
                  'description': 'The `Date` scalar type represents a timestamp provided in UTC. `Date` expects timestamps to be formatted in accordance with the [ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard.',
                  'fields': null
                },
                {
                  'kind': 'SCALAR',
                  'name': 'Decimal',
                  'description': null,
                  'fields': null
                },
                {
                  'kind': 'OBJECT',
                  'name': '__Schema',
                  'description': 'A GraphQL Schema defines the capabilities of a GraphQL server. It exposes all available types and directives on the server, as well as the entry points for query and mutation operations.',
                  'fields': [
                    {
                      'name': 'types',
                      'description': 'A list of all types supported by this server.',
                      'type': {
                        'name': null,
                        'kind': 'NON_NULL'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'queryType',
                      'description': 'The type that query operations will be rooted at.',
                      'type': {
                        'name': null,
                        'kind': 'NON_NULL'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'mutationType',
                      'description': 'If this server supports mutation, the type that mutation operations will be rooted at.',
                      'type': {
                        'name': '__Type',
                        'kind': 'OBJECT'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'directives',
                      'description': 'A list of all directives supported by this server.',
                      'type': {
                        'name': null,
                        'kind': 'NON_NULL'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    }
                  ]
                },
                {
                  'kind': 'OBJECT',
                  'name': '__Type',
                  'description': null,
                  'fields': [
                    {
                      'name': 'kind',
                      'description': null,
                      'type': {
                        'name': null,
                        'kind': 'NON_NULL'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'name',
                      'description': null,
                      'type': {
                        'name': 'String',
                        'kind': 'SCALAR'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'description',
                      'description': null,
                      'type': {
                        'name': 'String',
                        'kind': 'SCALAR'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'fields',
                      'description': null,
                      'type': {
                        'name': null,
                        'kind': 'LIST'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'interfaces',
                      'description': null,
                      'type': {
                        'name': null,
                        'kind': 'LIST'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'possibleTypes',
                      'description': null,
                      'type': {
                        'name': null,
                        'kind': 'LIST'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'enumValues',
                      'description': null,
                      'type': {
                        'name': null,
                        'kind': 'LIST'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'inputFields',
                      'description': null,
                      'type': {
                        'name': null,
                        'kind': 'LIST'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'ofType',
                      'description': null,
                      'type': {
                        'name': '__Type',
                        'kind': 'OBJECT'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    }
                  ]
                },
                {
                  'kind': 'ENUM',
                  'name': '__TypeKind',
                  'description': 'An enum describing what kind of type a given __Type is.',
                  'fields': null
                },
                {
                  'kind': 'OBJECT',
                  'name': '__Field',
                  'description': null,
                  'fields': [
                    {
                      'name': 'name',
                      'description': null,
                      'type': {
                        'name': null,
                        'kind': 'NON_NULL'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'description',
                      'description': null,
                      'type': {
                        'name': 'String',
                        'kind': 'SCALAR'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'args',
                      'description': null,
                      'type': {
                        'name': null,
                        'kind': 'NON_NULL'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'type',
                      'description': null,
                      'type': {
                        'name': null,
                        'kind': 'NON_NULL'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'isDeprecated',
                      'description': null,
                      'type': {
                        'name': null,
                        'kind': 'NON_NULL'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'deprecationReason',
                      'description': null,
                      'type': {
                        'name': 'String',
                        'kind': 'SCALAR'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    }
                  ]
                },
                {
                  'kind': 'OBJECT',
                  'name': '__InputValue',
                  'description': null,
                  'fields': [
                    {
                      'name': 'name',
                      'description': null,
                      'type': {
                        'name': null,
                        'kind': 'NON_NULL'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'description',
                      'description': null,
                      'type': {
                        'name': 'String',
                        'kind': 'SCALAR'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'type',
                      'description': null,
                      'type': {
                        'name': null,
                        'kind': 'NON_NULL'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'defaultValue',
                      'description': null,
                      'type': {
                        'name': 'String',
                        'kind': 'SCALAR'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    }
                  ]
                },
                {
                  'kind': 'OBJECT',
                  'name': '__EnumValue',
                  'description': null,
                  'fields': [
                    {
                      'name': 'name',
                      'description': null,
                      'type': {
                        'name': null,
                        'kind': 'NON_NULL'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'description',
                      'description': null,
                      'type': {
                        'name': 'String',
                        'kind': 'SCALAR'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'isDeprecated',
                      'description': null,
                      'type': {
                        'name': null,
                        'kind': 'NON_NULL'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'deprecationReason',
                      'description': null,
                      'type': {
                        'name': 'String',
                        'kind': 'SCALAR'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    }
                  ]
                },
                {
                  'kind': 'OBJECT',
                  'name': '__Directive',
                  'description': null,
                  'fields': [
                    {
                      'name': 'name',
                      'description': null,
                      'type': {
                        'name': null,
                        'kind': 'NON_NULL'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'description',
                      'description': null,
                      'type': {
                        'name': 'String',
                        'kind': 'SCALAR'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'args',
                      'description': null,
                      'type': {
                        'name': null,
                        'kind': 'NON_NULL'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'onOperation',
                      'description': null,
                      'type': {
                        'name': null,
                        'kind': 'NON_NULL'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'onFragment',
                      'description': null,
                      'type': {
                        'name': null,
                        'kind': 'NON_NULL'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'onField',
                      'description': null,
                      'type': {
                        'name': null,
                        'kind': 'NON_NULL'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    }
                  ]
                },
                {
                  'kind': 'OBJECT',
                  'name': 'Query',
                  'description': null,
                  'fields': [
                    {
                      'name': 'hero',
                      'description': null,
                      'type': {
                        'name': 'Character',
                        'kind': 'INTERFACE'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'human',
                      'description': null,
                      'type': {
                        'name': 'Human',
                        'kind': 'OBJECT'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'droid',
                      'description': null,
                      'type': {
                        'name': 'Droid',
                        'kind': 'OBJECT'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    }
                  ]
                },
                {
                  'kind': 'INTERFACE',
                  'name': 'Character',
                  'description': null,
                  'fields': [
                    {
                      'name': 'id',
                      'description': 'The id of the character.',
                      'type': {
                        'name': null,
                        'kind': 'NON_NULL'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'name',
                      'description': 'The name of the character.',
                      'type': {
                        'name': 'String',
                        'kind': 'SCALAR'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'friends',
                      'description': null,
                      'type': {
                        'name': null,
                        'kind': 'LIST'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'appearsIn',
                      'description': 'Which movie they appear in.',
                      'type': {
                        'name': null,
                        'kind': 'LIST'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    }
                  ]
                },
                {
                  'kind': 'ENUM',
                  'name': 'Episode',
                  'description': 'One of the films in the Star Wars Trilogy.',
                  'fields': null
                },
                {
                  'kind': 'OBJECT',
                  'name': 'Human',
                  'description': null,
                  'fields': [
                    {
                      'name': 'id',
                      'description': 'The id of the human.',
                      'type': {
                        'name': null,
                        'kind': 'NON_NULL'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'name',
                      'description': 'The name of the human.',
                      'type': {
                        'name': 'String',
                        'kind': 'SCALAR'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'friends',
                      'description': null,
                      'type': {
                        'name': null,
                        'kind': 'LIST'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'appearsIn',
                      'description': 'Which movie they appear in.',
                      'type': {
                        'name': null,
                        'kind': 'LIST'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'homePlanet',
                      'description': 'The home planet of the human.',
                      'type': {
                        'name': 'String',
                        'kind': 'SCALAR'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    }
                  ]
                },
                {
                  'kind': 'OBJECT',
                  'name': 'Droid',
                  'description': 'A mechanical creature in the Star Wars universe.',
                  'fields': [
                    {
                      'name': 'id',
                      'description': 'The id of the droid.',
                      'type': {
                        'name': null,
                        'kind': 'NON_NULL'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'name',
                      'description': 'The name of the droid.',
                      'type': {
                        'name': 'String',
                        'kind': 'SCALAR'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'friends',
                      'description': null,
                      'type': {
                        'name': null,
                        'kind': 'LIST'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'appearsIn',
                      'description': 'Which movie they appear in.',
                      'type': {
                        'name': null,
                        'kind': 'LIST'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    },
                    {
                      'name': 'primaryFunction',
                      'description': 'The primary function of the droid.',
                      'type': {
                        'name': 'String',
                        'kind': 'SCALAR'
                      },
                      'isDeprecated': false,
                      'deprecationReason': null
                    }
                  ]
                }
              ],
              'mutationType': null,
              'directives': [
                {
                  'name': 'include',
                  'description': 'Directs the executor to include this field or fragment only when the \'if\' argument is true.',
                  'onOperation': false,
                  'onFragment': true,
                  'onField': true
                },
                {
                  'name': 'skip',
                  'description': 'Directs the executor to skip this field or fragment when the \'if\' argument is true.',
                  'onOperation': false,
                  'onFragment': true,
                  'onField': true
                }
              ]
            }
            }";

            AssertQuerySuccess(query, expected);
        }
    }
}
