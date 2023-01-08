using GraphQL.Validation;
using GraphQL.Validation.Errors;
using GraphQLParser;

namespace GraphQL.Tests.StarWars;

public class StarWarsFragmentTests : StarWarsTestBase
{
    [Fact]
    public void use_fragment_spread_to_avoid_duplicate_content()
    {
        const string query = """
            query SomeDroids {
              r2d2: droid(id: "3") {
                ...DroidFragment
              }

              c3po: droid(id: "4") {
                ...DroidFragment
              }
            }
            fragment DroidFragment on Droid {
              name
            }
            """;

        const string expected = """
            {
              "r2d2": {
                "name": "R2-D2"
              },
              "c3po": {
                "name": "C-3PO"
              }
            }
            """;

        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void use_inline_fragment_on_interface()
    {
        const string query = """
            query SomeDroids {
              r2d2: droid(id: "3") {
                ... on Character {
                  name
                }
              }
            }
            """;

        const string expected = """
            {
              "r2d2": {
                "name": "R2-D2"
              }
            }
            """;

        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void use_unnamed_inline_fragment_on_interface()
    {
        const string query = """
            query SomeDroids {
              r2d2: droid(id: "3") {
                ... {
                  name
                }
              }
            }
            """;

        const string expected = """
            {
              "r2d2": {
                "name": "R2-D2"
              }
            }
            """;

        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void use_undefined_fragment()
    {
        const string query = """
            query someDroids {
              r2d2: droid(id: "3") {
                ...unknown_fragment
                name
              }
            }
            """;
        var errors = new ExecutionErrors();
        var error = new ValidationError(query, KnownFragmentNamesError.NUMBER, "Unknown fragment 'unknown_fragment'.")
        {
            Code = "KNOWN_FRAGMENT_NAMES"
        };
        error.AddLocation(new Location(3, 5));
        errors.Add(error);

        AssertQuery(query, CreateQueryResult(null, errors, executed: false), null, null);
    }
}
