using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class NoUnusedVariablesTests : ValidationTestBase<NoUnusedVariables, ValidationSchema>
{
    [Fact]
    public void uses_all_variables()
    {
        ShouldPassRule(@"
        query ($a: String, $b: String, $c: String) {
          field(a: $a, b: $b, c: $c)
        }
      ");
    }

    [Fact]
    public void uses_all_variables_deeply()
    {
        ShouldPassRule(@"
        query Foo($a: String, $b: String, $c: String) {
          field(a: $a) {
            field(b: $b) {
              field(c: $c)
            }
          }
        }
      ");
    }

    [Fact]
    public void uses_all_variables_deeply_in_inline_fragments()
    {
        ShouldPassRule(@"
        query Foo($a: String, $b: String, $c: String) {
          ... on Type {
            field(a: $a) {
              field(b: $b) {
                ... on Type {
                  field(c: $c)
                }
              }
            }
          }
        }
      ");
    }

    [Fact]
    public void uses_all_variables_in_fragments()
    {
        ShouldPassRule(@"
        query Foo($a: String, $b: String, $c: String) {
          ...FragA
        }
        fragment FragA on Type {
          field(a: $a) {
            ...FragB
          }
        }
        fragment FragB on Type {
          field(b: $b) {
            ...FragC
          }
        }
        fragment FragC on Type {
          field(c: $c)
        }
      ");
    }

    [Fact]
    public void variable_used_by_fragment_in_multiple_operations()
    {
        ShouldPassRule(@"
        query Foo($a: String) {
          ...FragA
        }
        query Bar($b: String) {
          ...FragB
        }
        fragment FragA on Type {
          field(a: $a)
        }
        fragment FragB on Type {
          field(b: $b)
        }
      ");
    }

    [Fact]
    public void variable_used_by_recursive_fragment()
    {
        ShouldPassRule(@"
        query Foo($a: String) {
          ...FragA
        }
        fragment FragA on Type {
          field(a: $a) {
            ...FragA
          }
        }
      ");
    }

    [Fact]
    public void variable_not_used()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
          query ($a: String, $b: String, $c: String) {
            field(a: $a, b: $b)
          }
        ";
            unusedVar(_, "c", null, 2, 42);
        });
    }

    [Fact]
    public void multiple_variables_not_used()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
          query Foo($a: String, $b: String, $c: String) {
            field(b: $b)
          }
        ";
            unusedVar(_, "a", "Foo", 2, 21);
            unusedVar(_, "c", "Foo", 2, 45);
        });
    }

    [Fact]
    public void variable_not_used_in_fragments()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
          query Foo($a: String, $b: String, $c: String) {
            ...FragA
          }
          fragment FragA on Type {
            field(a: $a) {
              ...FragB
            }
          }
          fragment FragB on Type {
            field(b: $b) {
              ...FragC
            }
          }
          fragment FragC on Type {
            field
          }
        ";
            unusedVar(_, "c", "Foo", 2, 45);
        });
    }

    [Fact]
    public void multiple_variables_not_used_InFragments()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
          query Foo($a: String, $b: String, $c: String) {
            ...FragA
          }
          fragment FragA on Type {
            field {
              ...FragB
            }
          }
          fragment FragB on Type {
            field(b: $b) {
              ...FragC
            }
          }
          fragment FragC on Type {
            field
          }
        ";
            unusedVar(_, "a", "Foo", 2, 21);
            unusedVar(_, "c", "Foo", 2, 45);
        });
    }

    [Fact]
    public void variable_not_used_by_unreferenced_fragment()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
          query Foo($b: String) {
            ...FragA
          }
          fragment FragA on Type {
            field(a: $a)
          }
          fragment FragB on Type {
            field(b: $b)
          }
        ";
            unusedVar(_, "b", "Foo", 2, 21);
        });
    }

    [Fact]
    public void variable_not_used_by_fragment_used_by_other_operation()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
          query Foo($b: String) {
            ...FragA
          }
          query Bar($a: String) {
            ...FragB
          }
          fragment FragA on Type {
            field(a: $a)
          }
          fragment FragB on Type {
            field(b: $b)
          }
        ";
            unusedVar(_, "b", "Foo", 2, 21);
            unusedVar(_, "a", "Bar", 5, 21);
        });
    }

    private void unusedVar(
      ValidationTestConfig _,
      string varName,
      string opName,
      int line,
      int column
      )
    {
        _.Error(err =>
        {
            err.Message = NoUnusedVariablesError.UnusedVariableMessage(varName, opName);
            err.Loc(line, column);
        });
    }
}
