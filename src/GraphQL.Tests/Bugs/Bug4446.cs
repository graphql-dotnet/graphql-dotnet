using GraphQL.Utilities;

namespace GraphQL.Tests.Bugs;

public class Bug4446
{
    [Fact]
    public async Task Include_False_On_Fragment_Spread_Inside_Defer_Inline_Fragment()
    {
        var sdl = """
            schema {
              query: Object_jGekrLP4U
            }

            directive @Directive_N on QUERY | SUBSCRIPTION | FRAGMENT_DEFINITION | FRAGMENT_SPREAD | VARIABLE_DEFINITION | SCALAR | OBJECT | FIELD_DEFINITION | INTERFACE | UNION | ENUM | ENUM_VALUE | INPUT_FIELD_DEFINITION

            directive @Directive_pN7J97aLjh on QUERY | FIELD | FIELD_DEFINITION | ARGUMENT_DEFINITION | ENUM | ENUM_VALUE | INPUT_OBJECT | INPUT_FIELD_DEFINITION

            directive @Directive_q on SUBSCRIPTION | FIELD_DEFINITION | INPUT_OBJECT

            "This directive allows results to be deferred during execution"
            directive @defer(
                "Deferred behaviour is controlled by this argument"
                if: Boolean! = true,
                "A unique label that represents the fragment being deferred"
                label: String
              ) on FRAGMENT_SPREAD | INLINE_FRAGMENT

            "Marks the field, argument, input field or enum value as deprecated"
            directive @deprecated(
                "The reason for the deprecation"
                reason: String! = "No longer supported"
              ) on FIELD_DEFINITION | ARGUMENT_DEFINITION | ENUM_VALUE | INPUT_FIELD_DEFINITION

            "This directive disables error propagation when a non nullable field returns null for the given operation."
            directive @experimental_disableErrorPropagation on QUERY | MUTATION | SUBSCRIPTION

            "Directs the executor to include this field or fragment only when the `if` argument is true"
            directive @include(
                "Included when true."
                if: Boolean!
              ) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT

            "Indicates an Input Object is a OneOf Input Object."
            directive @oneOf on INPUT_OBJECT

            "Directs the executor to skip this field or fragment when the `if` argument is true."
            directive @skip(
                "Skipped when true."
                if: Boolean!
              ) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT

            "Exposes a URL that specifies the behaviour of this scalar."
            directive @specifiedBy(
                "The URL that specifies the behaviour of this scalar."
                url: String!
              ) on SCALAR

            interface Interface_Jy1j {
              t_8: Interface_s6ENf
              vwI1f: Object_jGekrLP4U!
              vws: Object_nm48qsJ6Q7
            }

            interface Interface_OCZwJd {
              dBnUax: Union_V4vooz1kPF!
              pJ1: Interface_OCZwJd
            }

            interface Interface_s6ENf implements Interface_OCZwJd @Directive_N {
              dBnUax: Union_V4vooz1kPF!
              dKu: Object_jGekrLP4U
              pJ1: Interface_OCZwJd
              qYQtnCl: Int
              xC(lGguV: Enum_DeC3t1): Union_i
            }

            union Union_V4vooz1kPF = Object_nm48qsJ6Q7

            union Union_YFL = Object_jGekrLP4U | Object_nm48qsJ6Q7

            union Union_i = Object_jGekrLP4U

            type Interface_Jy1j_STUB implements Interface_Jy1j {
              cN: Float
              t_8: Interface_s6ENf
              vwI1f: Object_jGekrLP4U!
              vws: Object_nm48qsJ6Q7
            }

            type Interface_OCZwJd_STUB implements Interface_OCZwJd {
              bp6BKnuKB3: [Boolean]
              dBnUax: Union_V4vooz1kPF!
              pJ1: Interface_OCZwJd
              rd: String!
              vCD0TU: String
            }

            type Interface_s6ENf_STUB implements Interface_OCZwJd & Interface_s6ENf {
              dBnUax: Union_V4vooz1kPF!
              dKu: Object_jGekrLP4U
              gzOR2sk: Enum_DeC3t1
              iv: Union_i!
              pJ1: Interface_OCZwJd
              pak: Union_i
              qYQtnCl: Int
              xC(lGguV: Enum_DeC3t1): Union_i
            }

            type Object_QPe {
              ji(mD: Enum_JG1svr): Interface_Jy1j
            }

            type Object_jGekrLP4U {
              a: ID
              c5GCEDZ: ID! @Directive_pN7J97aLjh
              dQ6WtE34: Boolean
            }

            type Object_nm48qsJ6Q7 {
              _tFhCJXe(kl1Ej: [Input_npi]): Union_YFL @deprecated(reason : "|HA")
              gOZ: Enum_DeC3t1!
            }

            enum Enum_DeC3t1 {
              jNZ
            }

            enum Enum_JG1svr {
              IWogS @Directive_N @deprecated(reason : "p")
              fg4XINUZ
              srzX6AH
            }

            enum Enum_Q4AuLKtZ {
              sgNfXPqb6Z
              wlZB17pjn2
            }

            input Input_DRDr @oneOf {
              e6: Input_DRDr
              jOleAx: Enum_JG1svr
            }

            input Input_H_Hzu3hlf @oneOf {
              _M_Z: String
              hSw: [[Enum_JG1svr]]
            }

            input Input_npi {
              lM6hjd: Enum_Q4AuLKtZ
            }
            
            """;

        var sb = new SchemaBuilder();
        sb.Types.For("Interface_Jy1j_STUB").IsTypeOfFunc = _ => true;
        sb.Types.For("Interface_OCZwJd_STUB").IsTypeOfFunc = _ => true;
        sb.Types.For("Interface_s6ENf_STUB").IsTypeOfFunc = _ => true;
        sb.Types.For("Object_QPe").IsTypeOfFunc = _ => true;
        sb.Types.For("Object_jGekrLP4U").IsTypeOfFunc = _ => true;
        sb.Types.For("Object_nm48qsJ6Q7").IsTypeOfFunc = _ => true;
        var schema = sb.Build(sdl);
        schema.Initialize();

        var result = await schema.ExecuteAsync(o => o.Query = """
            query uxwzGlZi5 {
              ... on Union_i @defer(if: true, label: "Si") {
                ...Fragment_k1B
                ...Fragment_tsrkszkT
              }
            }

            fragment Fragment_kOqNbmg on Union_i {
              __typename
              __typename
            }

            fragment Fragment_k1B on Object_jGekrLP4U {
              ...Fragment_kOqNbmg@include(if: false)
            }

            fragment Fragment_tsrkszkT on Union_i @Directive_N{
              lPfrwoN: __typename
            }
            """);

        result.ShouldBeCrossPlatJson("""
            {
              "data": {
                "lPfrwoN": "Object_jGekrLP4U"
              }
            }
            """);
    }
}
