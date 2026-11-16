using GraphQL.Types;
using GraphQL.Validation;
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
}
