namespace GraphQL.Benchmarks
{
    public static class Queries
    {
        public static readonly string Introspection = @"
  query IntrospectionQuery {
    __schema {
      description
      queryType { name }
      mutationType { name }
      subscriptionType { name }
      types {
        ...FullType
      }
      directives {
        name
        description
        locations
        args {
          ...InputValue
        }
      }
    }
  }

  fragment FullType on __Type {
    kind
    name
    description
    fields(includeDeprecated: true) {
      name
      description
      args {
        ...InputValue
      }
      type {
        ...TypeRef
      }
      isDeprecated
      deprecationReason
    }
    inputFields {
      ...InputValue
    }
    interfaces {
      ...TypeRef
    }
    enumValues(includeDeprecated: true) {
      name
      description
      isDeprecated
      deprecationReason
    }
    possibleTypes {
      ...TypeRef
    }
  }

  fragment InputValue on __InputValue {
    name
    description
    type { ...TypeRef }
    defaultValue
  }

  fragment TypeRef on __Type {
    kind
    name
    ofType {
      kind
      name
      ofType {
        kind
        name
        ofType {
          kind
          name
          ofType {
            kind
            name
            ofType {
              kind
              name
              ofType {
                kind
                name
                ofType {
                  kind
                  name
                }
              }
            }
          }
        }
      }
    }
  }
";

        public static readonly string Hero = "{ hero { id name } }";

        public static readonly string Fragments = @"
query deep {
  human(id: ""abcd"") {
    friends {
      ... on Droid
        {
            primaryFunction
        friends
            {
                ... on Droid {
                    ...DroidData
                    __typename
                }
                ... on Human {
                    ...HumanData
                    __typename
                }
            }
        }
      ... on Human
        {
            homePlanet
        friends
            {
                ... on Droid {
                    ...DroidData
                    __typename
                }
                ... on Human {
                    ...HumanData
                    __typename
               }
            }
        }
    }
  }
}

fragment DroidData on Droid {
  primaryFunction
  name
}

fragment HumanData on Human {
  appearsIn
  homePlanet
}
";
    }
}
