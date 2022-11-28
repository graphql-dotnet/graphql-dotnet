using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class UniqueDirectivesPerLocationTests : ValidationTestBase<UniqueDirectivesPerLocation, ValidationSchema>
{
    [Fact]
    public void no_directives()
    {
        ShouldPassRule("""
                fragment Test on Type {
                    field
                }
            """);
    }

    [Fact]
    public void repeatable_directives_in_same_locations()
    {
        ShouldPassRule("""
                fragment Test on Type @rep @rep {
                    field @rep @rep
                }
            """);
    }

    [Fact]
    public void unique_directives_in_different_locations()
    {
        ShouldPassRule("""
                fragment Test on Type @directiveA {
                    field @directiveB
                }
            """);
    }

    [Fact]
    public void unique_directives_in_same_locations()
    {
        ShouldPassRule("""
                fragment Test on Type @directiveA @directiveB {
                    field @directiveA @directiveB
                }
            """);
    }

    [Fact]
    public void same_directives_in_different_locations()
    {
        ShouldPassRule("""
                fragment Test on Type @directiveA {
                    field @directiveA
                }
            """);
    }

    [Fact]
    public void same_directives_in_similar_locations()
    {
        ShouldPassRule("""
                fragment Test on Type {
                    field @directiveA
                    field @directiveA
                }
            """);
    }

    [Fact]
    public void duplicate_directives_in_one_location()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                fragment Test on Type {
                    field @directive @directive
                }
                """;
            duplicateDirective(_, "directive", 2, 11);
            duplicateDirective(_, "directive", 2, 22);
        });
    }

    [Fact]
    public void many_duplicate_directives_in_one_location()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                fragment Test on Type {
                    field @directive @directive @directive
                }
                """;
            duplicateDirective(_, "directive", 2, 11);
            duplicateDirective(_, "directive", 2, 22);
            duplicateDirective(_, "directive", 2, 33);
        });
    }

    [Fact]
    public void different_duplicate_directives_in_one_location()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                fragment Test on Type {
                    field @directiveA @directiveB @directiveA @directiveB
                }
                """;
            duplicateDirective(_, "directiveA", 2, 11);
            duplicateDirective(_, "directiveB", 2, 23);
            duplicateDirective(_, "directiveA", 2, 35);
            duplicateDirective(_, "directiveB", 2, 47);
        });
    }

    [Fact]
    public void duplicate_directives_in_many_locations()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                fragment Test on Type @directive @directive {
                    field @directive @directive
                }
                """;
            duplicateDirective(_, "directive", 1, 23);
            duplicateDirective(_, "directive", 1, 34);
            duplicateDirective(_, "directive", 2, 11);
            duplicateDirective(_, "directive", 2, 22);
        });
    }

    private void duplicateDirective(ValidationTestConfig _, string directiveName, int line, int column)
    {
        _.Error(err =>
        {
            err.Message = $"The directive '{directiveName}' can only be used once at this location.";
            err.Loc(line, column);
        });
    }
}
