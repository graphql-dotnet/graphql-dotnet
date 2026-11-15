using GraphQL.Validation;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Tests.Validation;

public class ValidationContextFragmentLookupTests
{
    [Fact]
    public void KeepsTheFirstDefinitionForDuplicateNames()
    {
        var document = Parser.Parse("""
            query { ...A }
            fragment A on Query { first }
            fragment A on Other { second }
            """);
        var first = (GraphQLFragmentDefinition)document.Definitions[1];
        var context = new ValidationContext { Document = document };

        context.FindFragmentDefinition(first.FragmentName.Name).ShouldBeSameAs(first);
        context.FindFragmentDefinition("missing").ShouldBeNull();
    }

    [Fact]
    public void LooksUpForwardReferencesAndMissingNamesInAnEmptyIndex()
    {
        var context = new ValidationContext { Document = Parser.Parse("query { ...missing ...other }") };

        context.FindFragmentDefinition("missing").ShouldBeNull();
        context.FindFragmentDefinition("other").ShouldBeNull();
    }

    [Fact]
    public void ClearsTheIndexWhenTheContextIsReused()
    {
        var context = new ValidationContext { Document = Parser.Parse("fragment A on Query { value }") };
        context.FindFragmentDefinition("A").ShouldNotBeNull();
        context.Reset();
        context.Document = Parser.Parse("fragment B on Query { value }");

        context.FindFragmentDefinition("A").ShouldBeNull();
        context.FindFragmentDefinition("B").ShouldNotBeNull();
    }
}

