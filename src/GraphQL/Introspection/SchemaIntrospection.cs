namespace GraphQL.Introspection
{
    public static class SchemaIntrospection
    {
        public static SchemaMetaFieldType SchemaMeta = new SchemaMetaFieldType();
        public static TypeMetaFieldType TypeMeta = new TypeMetaFieldType();
        public static TypeNameMetaFieldType TypeNameMeta = new TypeNameMetaFieldType();

        public static readonly string IntrospectionQuery = @"
        query IntrospectionQuery
        {
            __schema
            {
                queryType { name }
                mutationType { name }
                types {
                  ...FullType
                }
                directives {
                  name
                  description
                  args {
                    ...InputValue
                  }
                  onOperation
                  onFragment
                  onField
                }
            }
        }
        fragment FullType on __Type
        {
          kind
          name
          description
          fields {
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
            enumValues {
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
              }
            }
          }
        }
";

    }
}
