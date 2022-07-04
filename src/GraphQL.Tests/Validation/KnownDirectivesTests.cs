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
        ShouldPassRule(@"
              query Foo {
                name
                ...Frag
              }
              fragment Frag on Dog {
                name
              }
            ");
    }

    [Fact]
    public void with_known_directives()
    {
        ShouldPassRule(@"
              {
                dog @include(if: true) {
                  name
                }
                human @skip(if: false) {
                  name
                }
              }
            ");
    }

    [Fact]
    public void with_unknown_directives()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  {
                    dog @unknown(directive: ""value"") {
                      name
                    }
                  }
                ";
            unknownDirective(_, "unknown", 3, 25);
        });
    }

    [Fact]
    public void with_many_unknown_directives()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  {
                    dog @unknown(directive: ""value"") {
                      name
                    }
                    human @unknown(directive: ""value"") {
                      name
                      pets @unknown(directive: ""value"") {
                        name
                      }
                    }
                  }
                ";
            unknownDirective(_, "unknown", 3, 25);
            unknownDirective(_, "unknown", 6, 27);
            unknownDirective(_, "unknown", 8, 28);
        });
    }

    [Fact]
    public void with_well_placed_directives()
    {
        ShouldPassRule(@"
              query Foo @onQuery {
                name @include(if: true)
                ...Frag @include(if: true)
                skippedField @skip(if: true)
                ...SkippedFrag @skip(if: true)
              }

              mutation Bar @onMutation {
                someField
              }
            ");
    }

    [Fact]
    public void with_misplaced_directives()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  query Foo @include(if: true) {
                    name @onQuery
                    ...Frag @onQuery
                  }

                  mutation Bar @onQuery {
                    someField
                  }
                ";

            misplacedDirective(_, "include", DirectiveLocation.Query, 2, 29);
            misplacedDirective(_, "onQuery", DirectiveLocation.Field, 3, 26);
            misplacedDirective(_, "onQuery", DirectiveLocation.FragmentSpread, 4, 29);
            misplacedDirective(_, "onQuery", DirectiveLocation.Mutation, 7, 32);
        });
    }

    [Fact]
    public void within_schema_language_well_placed_directives()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
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
                ";
            unknownDirective(_, "onObject", 2, 53);
            unknownDirective(_, "onArgumentDefinition", 3, 40);
            unknownDirective(_, "onFieldDefinition", 3, 71);
            unknownDirective(_, "onScalar", 6, 35);
            unknownDirective(_, "onInterface", 8, 41);
            unknownDirective(_, "onArgumentDefinition", 9, 40);
            unknownDirective(_, "onFieldDefinition", 9, 71);
            unknownDirective(_, "onUnion", 12, 33);
            unknownDirective(_, "onEnum", 14, 31);
            unknownDirective(_, "onEnumValue", 15, 30);
            unknownDirective(_, "onInputObject", 18, 33);
            unknownDirective(_, "onInputFieldDefinition", 19, 34);
            unknownDirective(_, "onSchema", 22, 26);
        });
    }
}
