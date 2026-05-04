using GraphQL.Utilities;

namespace GraphQL.Tests.Bugs;

public class Bug4446
{
    [Fact]
    public async Task Include_False_On_Fragment_Spread_Inside_Defer_Inline_Fragment()
    {
        var sdl = """
            schema { query: Object_jGekrLP4U }
            directive @Directive_N on QUERY | SUBSCRIPTION | FRAGMENT_DEFINITION | FRAGMENT_SPREAD | VARIABLE_DEFINITION | SCALAR | OBJECT | FIELD_DEFINITION | INTERFACE | UNION | ENUM | ENUM_VALUE | INPUT_FIELD_DEFINITION
            "This directive allows results to be deferred during execution"
            directive @defer(
              "Deferred behaviour is controlled by this argument"
              if: Boolean! = true,
              "A unique label that represents the fragment being deferred"
              label: String
            ) on FRAGMENT_SPREAD | INLINE_FRAGMENT
            union Union_i = Object_jGekrLP4U
            type Object_jGekrLP4U {
              a: ID
              dQ6WtE34: Boolean
            }
            """;

        var sb = new SchemaBuilder();
        sb.Types.For("Union_i").ResolveType = _ => null!;
        var schema = sb.Build(sdl);
        schema.Initialize();

        var result = await schema.ExecuteAsync(o =>
        {
            o.Query = """
                query uxwzGlZi5 {
                  ... on Union_i @defer(if: true, label: "Si") {
                    ...Fragment_k1B
                    ...Fragment_tsrkszkT
                  }
                }
                fragment Fragment_kOqNbmg on Union_i { __typename __typename }
                fragment Fragment_k1B on Object_jGekrLP4U { ...Fragment_kOqNbmg @include(if: false) }
                fragment Fragment_tsrkszkT on Union_i @Directive_N { lPfrwoN: __typename }
                """;
        });

        result.ShouldBeCrossPlatJson("""
            {
              "data": {
                "lPfrwoN": "Object_jGekrLP4U"
              }
            }
            """);
    }
}
