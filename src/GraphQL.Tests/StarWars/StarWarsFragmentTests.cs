using GraphQL.Validation;
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
              ""r2d2"": {
                ""name"": ""R2-D2""
              },
              ""c3po"": {
                ""name"": ""C-3PO""
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
              ""r2d2"": {
                ""name"": ""R2-D2""
              }
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
              ""r2d2"": {
                ""name"": ""R2-D2""
              }
            }";

            AssertQuerySuccess(query, expected);
        }

        [Fact]
        public void use_undefined_fragment()
        {
            var query = @"
                query someDroids {
                    r2d2: droid(id: ""3"") {
                        ...unknown_fragment
                        name
                    }
               }
            ";
            var errors = new ExecutionErrors();
            var error = new ValidationError(query, "5.4.2.1", @"Unknown fragment ""unknown_fragment"".");
            error.AddLocation(4, 25);
            errors.Add(error);

            AssertQuery(query, CreateQueryResult(null, errors), null, null);
        }
    }
}
