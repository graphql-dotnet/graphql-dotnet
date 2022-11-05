using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class UniqueArgumentNamesTests : ValidationTestBase<UniqueArgumentNames, ValidationSchema>
{
    [Fact]
    public void no_arguments_on_field()
    {
        ShouldPassRule(@"
        {
          field
        }
      ");
    }

    [Fact]
    public void no_arguments_on_directive()
    {
        ShouldPassRule(@"
        {
          field @directive
        }
      ");
    }

    [Fact]
    public void argument_on_field()
    {
        ShouldPassRule(@"
        {
          field(arg: ""value"")
        }
      ");
    }

    [Fact]
    public void argument_on_directive()
    {
        ShouldPassRule(@"
        {
          field @directive(arg: ""value"")
        }
      ");
    }

    [Fact]
    public void same_argument_on_two_fields()
    {
        ShouldPassRule(@"
        {
          one: field(arg: ""value"")
          two: field(arg: ""value"")
        }
      ");
    }

    [Fact]
    public void same_argument_on_field_and_directive()
    {
        ShouldPassRule(@"
        {
          field(arg: ""value"") @directive(arg: ""value"")
        }
      ");
    }

    [Fact]
    public void same_argument_on_two_directives()
    {
        ShouldPassRule(@"
        {
          field @directive1(arg: ""value"") @directive2(arg: ""value"")
        }
      ");
    }

    [Fact]
    public void multiple_field_arguments()
    {
        ShouldPassRule(@"
        {
          field(arg1: ""value"", arg2: ""value"", arg3: ""value"")
        }
      ");
    }

    [Fact]
    public void multiple_directive_arguments()
    {
        ShouldPassRule(@"
        {
          field @directive(arg1: ""value"", arg2: ""value"", arg3: ""value"")
        }
      ");
    }

    [Fact]
    public void duplicate_field_arguments()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
          {
            field(arg1: ""value"", arg1: ""value"")
          }
        ";
            duplicateArg(_, "arg1", 3, 19, 3, 34);
        });
    }

    [Fact]
    public void many_duplicate_field_arguments()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
          {
            field(arg1: ""value"", arg1: ""value"", arg1: ""value"")
          }
        ";
            duplicateArg(_, "arg1", 3, 19, 3, 34);
            duplicateArg(_, "arg1", 3, 19, 3, 49);
        });
    }

    [Fact]
    public void duplicate_directive_arguments()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
          {
            field @directive(arg1: ""value"", arg1: ""value"")
          }
        ";
            duplicateArg(_, "arg1", 3, 30, 3, 45);
        });
    }

    [Fact]
    public void many_duplicate_directive_arguments()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
          {
            field @directive(arg1: ""value"", arg1: ""value"", arg1: ""value"")
          }
        ";
            duplicateArg(_, "arg1", 3, 30, 3, 45);
            duplicateArg(_, "arg1", 3, 30, 3, 60);
        });
    }

    private void duplicateArg(
      ValidationTestConfig _,
      string argName,
      int line1,
      int column1,
      int line2,
      int column2)
    {
        _.Error(err =>
        {
            err.Message = UniqueArgumentNamesError.DuplicateArgMessage(argName);
            err.Loc(line1, column1);
            err.Loc(line2, column2);
        });
    }
}
