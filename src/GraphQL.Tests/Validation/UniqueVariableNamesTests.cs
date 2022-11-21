using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class UniqueVariableNamesTests : ValidationTestBase<UniqueVariableNames, ValidationSchema>
{
    [Fact]
    public void unique_variable_names()
    {
        ShouldPassRule("""
            query A($x: Int, $y: String) { __typename }
            query B($x: String, $y: Int) { __typename }
            """);
    }

    [Fact]
    public void duplicate_variable_names()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                query A($x: Int, $x: Int, $x: String) { __typename }
                query B($x: String, $x: Int) { __typename }
                query C($x: Int, $x: Int) { __typename }
                """;
            duplicateVariable(_, "x", 1, 9, 1, 18);
            duplicateVariable(_, "x", 1, 9, 1, 27);
            duplicateVariable(_, "x", 2, 9, 2, 21);
            duplicateVariable(_, "x", 3, 9, 3, 18);
        });
    }

    private void duplicateVariable(
      ValidationTestConfig _,
      string variableName,
      int line1,
      int column1,
      int line2,
      int column2)
    {
        _.Error(err =>
        {
            err.Message = UniqueVariableNamesError.DuplicateVariableMessage(variableName);
            err.Loc(line1, column1);
            err.Loc(line2, column2);
        });
    }
}
