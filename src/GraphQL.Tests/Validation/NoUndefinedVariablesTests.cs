using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class NoUndefinedVariablesTests : ValidationTestBase<NoUndefinedVariables, ValidationSchema>
{
    [Fact]
    public void all_variables_defined()
    {
        ShouldPassRule(_ =>
        {
            _.Query = """
                  query Foo($a: String, $b: String, $c: String) {
                    field(a: $a, b: $b, c: $c)
                  }
                """;
        });
    }

    [Fact]
    public void all_variables_deeply_defined()
    {
        ShouldPassRule(_ =>
        {
            _.Query = """
                  query Foo($a: String, $b: String, $c: String) {
                    field(a: $a) {
                      field(b: $b) {
                        field(c: $c)
                      }
                    }
                  }
                """;
        });
    }

    [Fact]
    public void all_variables_deeply_in_inline_fragments_defined()
    {
        ShouldPassRule(_ =>
        {
            _.Query = """
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
                """;
        });
    }

    [Fact]
    public void all_variables_in_fragments_deeply_defined()
    {
        ShouldPassRule(_ =>
        {
            _.Query = """
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
                """;
        });
    }

    [Fact]
    public void variable_within_single_fragment_defined_in_multiple_operations()
    {
        ShouldPassRule(_ =>
        {
            _.Query = """
                  query Foo($a: String) {
                    ...FragA
                  }
                  query Bar($a: String) {
                    ...FragA
                  }
                  fragment FragA on Type {
                    field(a: $a)
                  }
                """;
        });
    }

    [Fact]
    public void variable_within_fragments_defined_in_operations()
    {
        ShouldPassRule(_ =>
        {
            _.Query = """
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
                """;
        });
    }

    [Fact]
    public void variable_within_recursive_fragment_defined()
    {
        ShouldPassRule(_ =>
        {
            _.Query = """
                  query Foo($a: String) {
                    ...FragA
                  }
                  fragment FragA on Type {
                    field(a: $a) {
                      ...FragA
                    }
                  }
                """;
        });
    }

    [Fact]
    public void variable_not_defined()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                  query Foo($a: String, $b: String, $c: String) {
                    field(a: $a, b: $b, c: $c, d: $d)
                  }
                """;
            undefVar(_, "d", 2, 35, "Foo", 1, 3);
        });
    }

    [Fact]
    public void variable_not_defined_by_unnamed_query()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                  {
                    field(a: $a)
                  }
                """;
            undefVar(_, "a", 2, 14, "", 1, 3);
        });
    }

    [Fact]
    public void multiple_variables_not_defined()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                  query Foo($b: String) {
                    field(a: $a, b: $b, c: $c)
                  }
                """;
            undefVar(_, "a", 2, 14, "Foo", 1, 3);
            undefVar(_, "c", 2, 28, "Foo", 1, 3);
        });
    }

    [Fact]
    public void variable_in_fragment_not_defined_by_unnamed_query()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                  {
                    ...FragA
                  }
                  fragment FragA on Type {
                    field(a: $a)
                  }
                """;
            undefVar(_, "a", 5, 14, "", 1, 3);
        });
    }

    [Fact]
    public void variable_in_fragment_not_defined_by_operation()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                  query Foo($a: String, $b: String) {
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
                """;
            undefVar(_, "c", 15, 14, "Foo", 1, 3);
        });
    }

    [Fact]
    public void multiple_variables_in_fragments_not_defined()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                  query Foo($b: String) {
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
                """;
            undefVar(_, "a", 5, 14, "Foo", 1, 3);
            undefVar(_, "c", 15, 14, "Foo", 1, 3);
        });
    }

    [Fact]
    public void single_variable_in_fragment_not_defined_by_multiple_operations()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                  query Foo($a: String) {
                    ...FragAB
                  }
                  query Bar($a: String) {
                    ...FragAB
                  }
                  fragment FragAB on Type {
                    field(a: $a, b: $b)
                  }
                """;
            undefVar(_, "b", 8, 21, "Foo", 1, 3);
            undefVar(_, "b", 8, 21, "Bar", 4, 3);
        });
    }

    [Fact]
    public void variables_in_fragment_not_defined_by_multiple_operations()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                  query Foo($b: String) {
                    ...FragAB
                  }
                  query Bar($a: String) {
                    ...FragAB
                  }
                  fragment FragAB on Type {
                    field(a: $a, b: $b)
                  }
                """;
            undefVar(_, "a", 8, 14, "Foo", 1, 3);
            undefVar(_, "b", 8, 21, "Bar", 4, 3);
        });
    }

    [Fact]
    public void variable_in_fragment_used_by_other_operation()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
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
                """;
            undefVar(_, "a", 8, 14, "Foo", 1, 3);
            undefVar(_, "b", 11, 14, "Bar", 4, 3);
        });
    }

    [Fact]
    public void multiple_undefined_variables_produce_multiple_errors()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                  query Foo($b: String) {
                    ...FragAB
                  }
                  query Bar($a: String) {
                    ...FragAB
                  }
                  fragment FragAB on Type {
                    field1(a: $a, b: $b)
                    ...FragC
                    field3(a: $a, b: $b)
                  }
                  fragment FragC on Type {
                    field2(c: $c)
                  }
                """;
            undefVar(_, "a", 8, 15, "Foo", 1, 3);
            undefVar(_, "a", 10, 15, "Foo", 1, 3);
            undefVar(_, "c", 13, 15, "Foo", 1, 3);
            undefVar(_, "b", 8, 22, "Bar", 4, 3);
            undefVar(_, "b", 10, 22, "Bar", 4, 3);
            undefVar(_, "c", 13, 15, "Bar", 4, 3);
        });
    }

    private void undefVar(
        ValidationTestConfig _,
        string varName,
        int line1,
        int column1,
        string opName,
        int line2,
        int column2)
    {
        _.Error(err =>
        {
            err.Message = NoUndefinedVariablesError.UndefinedVarMessage(varName, opName);
            err.Loc(line1, column1);
            err.Loc(line2, column2);
        });
    }
}
