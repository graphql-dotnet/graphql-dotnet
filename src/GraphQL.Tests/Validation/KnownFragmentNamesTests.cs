using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class KnownFragmentNamesTests : ValidationTestBase<KnownFragmentNames, ValidationSchema>
{
    [Fact]
    public void known_fragment_names_are_valid()
    {
        ShouldPassRule("""
        {
          human(id: 4) {
            ...HumanFields1
            ... on Human {
              ...HumanFields2
            }
            ... {
              name
            }
          }
        }
        fragment HumanFields1 on Human {
          name
          ...HumanFields3
        }
        fragment HumanFields2 on Human {
          name
        }
        fragment HumanFields3 on Human {
          name
        }
      """);
    }

    [Fact]
    public void unknown_fragment_names_are_invalid()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
            {
              human(id: 4) {
                ...UnknownFragment1
                ... on Human {
                ...UnknownFragment2
                }
              }
            }
            fragment HumanFields on Human {
              name
              ...UnknownFragment3
            }
            """;
            undefFrag(_, "UnknownFragment1", 3, 5);
            undefFrag(_, "UnknownFragment2", 5, 5);
            undefFrag(_, "UnknownFragment3", 11, 3);
        });
    }

    private void undefFrag(
      ValidationTestConfig _,
      string fragName,
      int line,
      int column)
    {
        _.Error(err =>
        {
            err.Message = KnownFragmentNamesError.UnknownFragmentMessage(fragName);
            err.Loc(line, column);
        });
    }
}
