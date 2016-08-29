using Xunit;

namespace GraphQL.Tests.StarWars
{
    public class StarWarsFragmentTests : StarWarsTestBase
    {
        [Fact]
        public void use_fragment_spread_to_avoid_duplicate_content()
        {
            var query = @"
               query SomeDroids {
                  r2d2: droid(id: ""3"") {
                    ...DroidFragment
                  }

                  c3po: droid(id: ""4"") {
                    ...DroidFragment
                  }
               }
               fragment DroidFragment on Droid {
                 name
               }
            ";

            var expected = @"{
              'r2d2': {
                name: 'R2-D2'
              },
              'c3po': {
                name: 'C-3PO'
              }
            }";

            AssertQuerySuccess(query, expected);
        }

        [Fact]
        public void use_inline_fragment_on_interface()
        {
            var query = @"
               query SomeDroids {
                  r2d2: droid(id: ""3"") {
                    ... on Character {
                      name
                    }
                  }
               }
            ";

            var expected = @"{
              'r2d2': {
                name: 'R2-D2'
              },
            }";

            AssertQuerySuccess(query, expected);
        }

        [Fact]
        public void use_unnamed_inline_fragment_on_interface()
        {
            var query = @"
               query SomeDroids {
                  r2d2: droid(id: ""3"") {
                    ... {
                      name
                    }
                  }
               }
            ";

            var expected = @"{
              'r2d2': {
                name: 'R2-D2'
              },
            }";

            AssertQuerySuccess(query, expected);
        }
    }
}
