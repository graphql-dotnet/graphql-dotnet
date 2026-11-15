using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Tests.Validation;

public class ParseArgumentVisitorTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Parses_nested_and_repeated_fragments_for_selected_operation_only(bool useRules)
    {
        var result = await ValidateAsync("""
            query Other @tag(value: 99) {
              ...OtherFields
            }
            query Selected($value: Int! = 10) @tag(value: 10) {
              direct: echo(value: $value)
              ...First
              ...Second
            }
            fragment First on Query {
              first: echo(value: 1)
              ...Shared
            }
            fragment Second on Query {
              second: echo(value: 2)
              ...Shared
            }
            fragment Shared on Query {
              shared: echo(value: 3)
            }
            fragment OtherFields on Query {
              other: echo(value: 99)
            }
            """, useRules);

        result.IsValid.ShouldBeTrue();
        var arguments = result.ArgumentValues.ShouldNotBeNull()
            .ToDictionary(x => x.Key.Alias!.Name.StringValue, x => x.Value["value"].Value);
        arguments.Count.ShouldBe(4);
        arguments["direct"].ShouldBe(10);
        arguments["first"].ShouldBe(1);
        arguments["second"].ShouldBe(2);
        arguments["shared"].ShouldBe(3);
        var directive = result.DirectiveValues.ShouldNotBeNull().ShouldHaveSingleItem();
        var operation = directive.Key.ShouldBeAssignableTo<GraphQLOperationDefinition>();
        operation.ShouldNotBeNull().Name!.StringValue.ShouldBe("Selected");
        directive.Value["tag"].Arguments["value"].Value.ShouldBe(10);
    }

    [Theory]
    [InlineData(true, "skip", true)]
    [InlineData(false, "skip", true)]
    [InlineData(true, "include", false)]
    [InlineData(false, "include", false)]
    public async Task Parses_fragments_excluded_by_directives_and_preserves_directive_values(bool useRules, string directiveName, bool directiveValue)
    {
        string directive = $"@{directiveName}(if: {(directiveValue ? "true" : "false")})";
        var result = await ValidateAsync($$"""
            query Selected {
              ...Outer {{directive}}
              ... on Query {{directive}} {
                ...Inline
              }
              nested {{directive}} {
                ...Nested
              }
            }
            fragment Outer on Query @tag(value: 11) {
              first: echo(value: 1)
              ...Inner {{directive}}
            }
            fragment Inner on Query {
              second: echo(value: 2)
            }
            fragment Inline on Query {
              third: echo(value: 3)
            }
            fragment Nested on Query {
              fourth: echo(value: 4)
            }
            """, useRules);

        result.IsValid.ShouldBeTrue();
        result.ArgumentValues.ShouldNotBeNull().Values
            .Select(x => x["value"].Value.ShouldBeOfType<int>())
            .OrderBy(x => x)
            .ShouldBe(new[] { 1, 2, 3, 4 });
        var directives = result.DirectiveValues.ShouldNotBeNull();
        directives.Count.ShouldBe(5);
        var spreadDirectives = directives.Where(x => x.Key is GraphQLFragmentSpread).ToList();
        spreadDirectives.Count.ShouldBe(2);
        foreach (var spreadDirective in spreadDirectives)
            spreadDirective.Value[directiveName].Arguments["if"].Value.ShouldBe(directiveValue);
        var fragmentDirective = directives.Single(x => x.Key is GraphQLFragmentDefinition);
        fragmentDirective.Value["tag"].Arguments["value"].Value.ShouldBe(11);
    }

    [Fact]
    public async Task Parses_cyclic_fragments_once_when_validation_rules_are_disabled()
    {
        var result = await ValidateAsync("""
            query Selected {
              ...First
            }
            fragment First on Query {
              first: echo(value: 1)
              ...Second
            }
            fragment Second on Query {
              second: echo(value: 2)
              ...First
            }
            """, useRules: false);

        result.IsValid.ShouldBeTrue();
        result.ArgumentValues.ShouldNotBeNull().Values
            .Select(x => x["value"].Value.ShouldBeOfType<int>())
            .OrderBy(x => x)
            .ShouldBe(new[] { 1, 2 });
    }

    private static async Task<IValidationResult> ValidateAsync(string query, bool useRules)
    {
        using var schema = Schema.For("""
            directive @tag(value: Int!) on QUERY | FRAGMENT_DEFINITION
            type Query {
              echo(value: Int!): Int
              nested: Query
            }
            """);
        schema.Initialize();
        var document = Parser.Parse(query);
        return await new DocumentValidator().ValidateAsync(new()
        {
            Document = document,
            Schema = schema,
            Operation = document.Definitions.OfType<GraphQLOperationDefinition>().Single(x => x.Name?.StringValue == "Selected"),
            Rules = useRules ? null : Array.Empty<IValidationRule>(),
            Variables = Inputs.Empty,
        });
    }
}
