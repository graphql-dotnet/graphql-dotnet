using System.Text;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Tests.Validation;

public class NoUnusedFragmentsReachabilityTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public async Task VisitsADeepSharedCyclicChainAcrossOperations(int operationCount)
    {
        const int count = 10_000;
        var query = new StringBuilder();
        for (int i = 0; i < operationCount; ++i)
            query.Append("query Q").Append(i).AppendLine(" { child { ...F0 } ... on Query { ...F0 @skip(if: true) } }");
        for (int i = 0; i < count; ++i)
            query.Append("fragment F").Append(i).Append(" on Query { value ...F").Append((i + 1) % count).AppendLine(" }");

        var context = await Validate(query.ToString());
        context.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task PreservesDuplicateDefinitionIdentityAndUnusedErrorOrder()
    {
        var context = await Validate("""
            query { ...A ...missing }
            fragment A on Query { ...B }
            fragment A on Query { ...Unused }
            fragment B on Query { ...A }
            fragment Unused on Query { ...Unused }
            """);

        context.Errors.Select(x => x.Message).ShouldBe([
            NoUnusedFragmentsError.UnusedFragMessage("A"),
            NoUnusedFragmentsError.UnusedFragMessage("Unused"),
        ]);
        context.Errors.Select(x => x.Locations!.Single().Line).ShouldBe([3, 5]);
    }

    [Fact]
    public async Task PreservesBehaviorWithoutOperations()
    {
        var context = await Validate("fragment A on Query { value }");
        context.Errors.ShouldBeEmpty();
    }

    private static async Task<ValidationContext> Validate(string query)
    {
        var schema = Schema.For("type Query { value: String, child: Query }");
        schema.Initialize();
        var document = Parser.Parse(query);
        var context = new ValidationContext
        {
            Schema = schema,
            Document = document,
            TypeInfo = new TypeInfo(schema),
        };
        var visitor = await NoUnusedFragments.Instance.GetPreNodeVisitorAsync(context);
        await new BasicVisitor(context.TypeInfo, visitor!).VisitAsync(document, new BasicVisitor.State(context));
        return context;
    }
}

