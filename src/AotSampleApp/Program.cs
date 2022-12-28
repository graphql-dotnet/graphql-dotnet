// See https://aka.ms/new-console-template for more information
using System.Diagnostics.CodeAnalysis;
using AotSampleApp;
using GraphQL;
using GraphQL.StarWars;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Hello, World!");
Console.WriteLine();

IServiceCollection serviceCollection = new ServiceCollection();
// build services as usual; most methods are fully supported except:
//   - AddClrTypeMappings
//   - AddAutoClrMappings
//   - AddAutoSchema
serviceCollection.AddGraphQL(b => b
    .AddSystemTextJson()
    //.AddSchema<StarWarsSchema>()
    //.AddGraphTypes(typeof(StarWarsSchema).Assembly)
    .AddSelfActivatingSchema<StarWarsSchema>()
);

serviceCollection.AddSingleton<StarWarsData>();

// must manually preserve the query and mutation types or AOT will trim their constructors
// all other graph types' constructors are preserved via calls to Field<T>
Preserve<StarWarsQuery>();
Preserve<StarWarsMutation>();
// must manually preserve any GraphQL type references
Preserve<GraphQLClrInputTypeReference<string>>();

// other notes:
// - clr type mappings are generally not supported
// - auto registering graph types are generally not supported
// - field builders that use Expressions as resolvers such as Field(x => x.Name) are discouraged
// - field builders that do not include a resolver such as Field<StringGraphType>("Name") is not supported
// - strongly recommend each field has the type specified and a resolver specified

// must use AOT-friendly service provider
// note: does not support open generics, such as AutoRegisteringObjectGraphType<>
// note: does not exhaustively search constructors, but rather uses the first one defined

var services = AotServiceProvider.Create(serviceCollection);

var executer = services.GetRequiredService<IDocumentExecuter>();

var ret = await executer.ExecuteAsync(new ExecutionOptions
{
    Schema = services.GetRequiredService<ISchema>(),
    Query = "{ hero { id name } }",
    RequestServices = services,
    ThrowOnUnhandledException = true,
}).ConfigureAwait(false);

var serializer = services.GetRequiredService<IGraphQLTextSerializer>();
var response = serializer.Serialize(ret);

Console.WriteLine(response);

Console.WriteLine();

var introspectionQuery = """
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
""";


ret = await executer.ExecuteAsync(new ExecutionOptions
{
    Schema = services.GetRequiredService<ISchema>(),
    Query = introspectionQuery,
    RequestServices = services,
    ThrowOnUnhandledException = true,
}).ConfigureAwait(false);

response = serializer.Serialize(ret);

Console.WriteLine(response);

// forces AOT to preserve the specified type's definition and public constructors
static void Preserve<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
    => GC.KeepAlive(typeof(T)); // GC.KeepAlive forces the type to be preserved

if (ret.Errors == null)
    throw new ApplicationException("test");

// return an application exit code if there were any errors
return ret.Errors?.Count ?? 0;
