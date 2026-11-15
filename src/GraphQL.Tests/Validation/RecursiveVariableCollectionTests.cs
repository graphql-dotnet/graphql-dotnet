using System.Text;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Rules;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Tests.Validation;

public class RecursiveVariableCollectionTests
{
    [Fact]
    public void PreservesTypesAndDefaultsBetweenFragmentsAndOperations()
    {
        var schema = Schema.For("""
            type Query {
              text(arg: String = "default"): String
              number(arg: Int!): Int
            }
            """);
        schema.Initialize();
        var document = Parser.Parse("""
            query First($text: String, $number: Int!) { ...A ...B }
            query Second($text: String, $number: Int!) { ...B ...A }
            fragment A on Query { text(arg: $text) ...B }
            fragment B on Query { number(arg: $number) ...A }
            """);
        var context = new ValidationContext { Schema = schema, Document = document };
        var first = context.GetRecursiveVariables((GraphQLOperationDefinition)document.Definitions[0])!;
        var second = context.GetRecursiveVariables((GraphQLOperationDefinition)document.Definitions[1])!;

        first.Select(x => x.Node.Name.StringValue).ShouldBe(["text", "number"]);
        first.Select(x => x.Type.ToString()).ShouldBe(["String", "Int!"]);
        first.Select(x => x.HasDefault).ShouldBe([true, false]);
        second.Select(x => x.Node.Name.StringValue).ShouldBe(["number", "text"]);
        second.Select(x => x.Type.ToString()).ShouldBe(["Int!", "String"]);
        second.Select(x => x.HasDefault).ShouldBe([false, true]);
        context.GetRecursiveVariables((GraphQLOperationDefinition)document.Definitions[0]).ShouldBeSameAs(first);
    }

    [Fact]
    public async Task ReportsEachUndefinedUsageOnceInADeepCyclicChain()
    {
        const int count = 10_000;
        var query = new StringBuilder("query Test { ...F0 ...F0 }\n");
        for (int i = 0; i < count; ++i)
        {
            query.Append("fragment F").Append(i).Append(" on Query { ...F").Append((i + 1) % count);
            if (i == 0 || i == count - 1)
                query.Append(" value @include(if: $missing)");
            query.AppendLine(" }");
        }
        var schema = Schema.For("type Query { value: String }");
        schema.Initialize();
        var document = Parser.Parse(query.ToString());
        var context = new ValidationContext
        {
            Schema = schema,
            Document = document,
            TypeInfo = new TypeInfo(schema),
        };
        var visitor = await NoUndefinedVariables.Instance.GetPreNodeVisitorAsync(context);
        await new BasicVisitor(context.TypeInfo, visitor!).VisitAsync(document, new BasicVisitor.State(context));

        context.Errors.Count().ShouldBe(2);
        context.Errors.Select(x => x.Locations!.First().Line).ShouldBe([2, count + 1]);
    }

    [Fact]
    public void DuplicateNamesStillUseTheFirstDefinition()
    {
        var schema = Schema.For("type Query { value: String }");
        schema.Initialize();
        var document = Parser.Parse("""
            query { ...A }
            fragment A on Query { value @include(if: $first) }
            fragment A on Query { value @include(if: $second) }
            """);
        var context = new ValidationContext { Schema = schema, Document = document };

        context.GetRecursiveVariables((GraphQLOperationDefinition)document.Definitions[0])!
            .Select(x => x.Node.Name.StringValue).ShouldBe(["first"]);
    }
}

