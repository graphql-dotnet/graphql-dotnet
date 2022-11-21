using GraphQL.Validation.Rules;
using GraphQLParser.AST;

namespace GraphQL.Tests.Validation;

public class KnownDirectivesTests : ValidationTestBase<KnownDirectivesInAllowedLocations, ValidationSchema>
{
    private void unknownDirective(ValidationTestConfig _, string name, int line, int column)
    {
        _.Error($"Unknown directive '{name}'.", line, column);
    }

    private void misplacedDirective(ValidationTestConfig _, string name, DirectiveLocation placement, int line, int column)
    {
        _.Error($"Directive '{name}' may not be used on {placement}.", line, column);
    }

    [Fact]
    public void with_no_directives()
    {
        ShouldPassRule("""
              query Foo {
                name
                ...Frag
              }
              fragment Frag on Dog {
                name
              }
            """);
    }

    [Fact]
    public void with_known_directives()
    {
        ShouldPassRule("""
              {
                dog @include(if: true) {
                  name
                }
                human @skip(if: false) {
                  name
                }
              }
            """);
    }

    [Fact]
    public void with_unknown_directives()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                  {
                    dog @unknown(directive: "value") {
                      name
                    }
                  }
                """;
            unknownDirective(_, "unknown", 2, 9);
        });
    }

    [Fact]
    public void with_many_unknown_directives()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                  {
                    dog @unknown(directive: "value") {
                      name
                    }
                    human @unknown(directive: "value") {
                      name
                      pets @unknown(directive: "value") {
                        name
                      }
                    }
                  }
                """;
            unknownDirective(_, "unknown", 2, 9);
            unknownDirective(_, "unknown", 5, 11);
            unknownDirective(_, "unknown", 7, 12);
        });
    }

    [Fact]
    public void with_well_placed_directives()
    {
        ShouldPassRule("""
              query Foo @onQuery {
                name @include(if: true)
                ...Frag @include(if: true)
                skippedField @skip(if: true)
                ...SkippedFrag @skip(if: true)
              }

              mutation Bar @onMutation {
                someField
              }
            """);
    }

    [Fact]
    public void with_misplaced_directives()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                  query Foo @include(if: true) {
                    name @onQuery
                    ...Frag @onQuery
                  }

                  mutation Bar @onQuery {
                    someField
                  }
                """;

            misplacedDirective(_, "include", DirectiveLocation.Query, 1, 13);
            misplacedDirective(_, "onQuery", DirectiveLocation.Field, 2, 10);
            misplacedDirective(_, "onQuery", DirectiveLocation.FragmentSpread, 3, 13);
            misplacedDirective(_, "onQuery", DirectiveLocation.Mutation, 6, 16);
        });
    }

    [Fact]
    public void within_schema_language_well_placed_directives()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                  type MyObj implements MyInterface @onObject {
                    myField(myArg: Int @onArgumentDefinition): String @onFieldDefinition
                  }

                  scalar MyScalar @onScalar

                  interface MyInterface @onInterface {
                    myField(myArg: Int @onArgumentDefinition): String @onFieldDefinition
                  }

                  union MyUnion @onUnion = MyObj | Other

                  enum MyEnum @onEnum {
                    MY_VALUE @onEnumValue
                  }

                  input MyInput @onInputObject {
                    myField: Int @onInputFieldDefinition
                  }

                  schema @onSchema {
                    query: MyQuery
                  }
                """;
            unknownDirective(_, "onObject", 1, 37);
            unknownDirective(_, "onArgumentDefinition", 2, 24);
            unknownDirective(_, "onFieldDefinition", 2, 55);
            unknownDirective(_, "onScalar", 5, 19);
            unknownDirective(_, "onInterface", 7, 25);
            unknownDirective(_, "onArgumentDefinition", 8, 24);
            unknownDirective(_, "onFieldDefinition", 8, 55);
            unknownDirective(_, "onUnion", 11, 17);
            unknownDirective(_, "onEnum", 13, 15);
            unknownDirective(_, "onEnumValue", 14, 14);
            unknownDirective(_, "onInputObject", 17, 17);
            unknownDirective(_, "onInputFieldDefinition", 18, 18);
            unknownDirective(_, "onSchema", 21, 10);
        });
    }
}
