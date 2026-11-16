using GraphQL.Validation;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Tests.Validation;

public class ValidationContextFragmentLookupTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 0)]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    [InlineData(0, 3)]
    public void FindsFragmentsByNameAndIgnoresOtherDefinitions(int fragmentCount, int operationCount)
    {
        var document = CreateDocument(fragmentCount, operationCount);
        var context = new ValidationContext { Document = document };

        // A miss can be the first lookup, including a name used only by an operation.
        context.FindFragmentDefinition($"F{fragmentCount}").ShouldBeNull();
        foreach (var fragment in document.Definitions.OfType<GraphQLFragmentDefinition>())
        {
            context.FindFragmentDefinition(fragment.FragmentName.Name.StringValue).ShouldBeSameAs(fragment);
            context.FindFragmentDefinition(fragment.FragmentName.Name.StringValue).ShouldBeSameAs(fragment);
        }
        context.FindFragmentDefinition($"F{fragmentCount}").ShouldBeNull();
        context.FindFragmentDefinition("f0").ShouldBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void KeepsTheFirstDefinitionForDuplicateNames(bool includeOperation)
    {
        var document = Parser.Parse((includeOperation ? "query { ...A }\n" : "") + """
            fragment A on Query { first }
            fragment A on Other { second }
            """);
        var first = document.Definitions.OfType<GraphQLFragmentDefinition>().First();
        var context = new ValidationContext { Document = document };

        context.FindFragmentDefinition("A").ShouldBeSameAs(first);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 3)]
    [InlineData(3, 1)]
    [InlineData(3, 3)]
    public void ClearsTheIndexWhenTheContextIsReused(int firstFragmentCount, int secondFragmentCount)
    {
        var context = new ValidationContext { Document = CreateDocument(firstFragmentCount, 0, "Old") };
        context.FindFragmentDefinition("Old0").ShouldNotBeNull();
        context.FindFragmentDefinition("New0").ShouldBeNull();
        context.Reset();
        var document = CreateDocument(secondFragmentCount, 0, "New");
        context.Document = document;

        context.FindFragmentDefinition("Old0").ShouldBeNull();
        context.FindFragmentDefinition("New0").ShouldBeSameAs(document.Definitions[0]);
    }

    private static GraphQLDocument CreateDocument(int fragmentCount, int operationCount, string prefix = "F")
    {
        var definitions = new List<string>();
        for (int i = 0; i < operationCount; ++i)
            definitions.Add($"query F{i} {{ value }}");
        for (int i = 0; i < fragmentCount; ++i)
            definitions.Add($"fragment {prefix}{i} on Query {{ value }}");

        return definitions.Count == 0 ? new GraphQLDocument([]) : Parser.Parse(string.Join("\n", definitions));
    }
}

