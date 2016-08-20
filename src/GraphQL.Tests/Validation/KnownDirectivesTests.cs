using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation
{
    public class KnownDirectivesTests : ValidationTestBase<KnownDirectives, ValidationSchema>
    {
        private void unknownDirective(ValidationTestConfig _, string name, int line, int column)
        {
            _.Error(KnownDirectives.UnknownDirectiveMessage(name), line, column);
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
    }
}
