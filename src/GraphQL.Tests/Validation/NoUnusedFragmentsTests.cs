using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class NoUnusedFragmentsTests : ValidationTestBase<NoUnusedFragments, ValidationSchema>
{
    [Fact]
    public void all_fragment_names_are_used()
    {
        ShouldPassRule(@"
        {
          human(id: 4) {
            ...HumanFields1
            ... on Human {
              ...HumanFields2
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
      ");
    }

    [Fact]
    public void all_fragment_names_are_used_by_multiple_operations()
    {
        ShouldPassRule(@"
        query Foo {
          human(id: 4) {
            ...HumanFields1
          }
        }
        query Bar {
          human(id: 4) {
            ...HumanFields2
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
      ");
    }

    [Fact]
    public void contains_unknown_fragments()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
          query Foo {
            human(id: 4) {
              ...HumanFields1
            }
          }
          query Bar {
            human(id: 4) {
              ...HumanFields2
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
          fragment Unused1 on Human {
            name
          }
          fragment Unused2 on Human {
            name
          }
        ";
            unusedFrag(_, "Unused1", 22, 11);
            unusedFrag(_, "Unused2", 25, 11);
        });
    }

    [Fact]
    public void contains_unknown_fragments_with_ref_cycle()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
          query Foo {
            human(id: 4) {
              ...HumanFields1
            }
          }
          query Bar {
            human(id: 4) {
              ...HumanFields2
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
          fragment Unused1 on Human {
            name
            ...Unused2
          }
          fragment Unused2 on Human {
            name
            ...Unused1
          }
        ";
            unusedFrag(_, "Unused1", 22, 11);
            unusedFrag(_, "Unused2", 26, 11);
        });
    }

    [Fact]
    public void contains_unknown_and_undef_fragments()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
          query Foo {
            human(id: 4) {
              ...bar
            }
          }
          fragment foo on Human {
            name
          }
        ";
            unusedFrag(_, "foo", 7, 11);
        });
    }

    private void unusedFrag(
      ValidationTestConfig _,
      string varName,
      int line,
      int column
      )
    {
        _.Error(err =>
        {
            err.Message = NoUnusedFragmentsError.UnusedFragMessage(varName);
            err.Loc(line, column);
        });
    }
}
