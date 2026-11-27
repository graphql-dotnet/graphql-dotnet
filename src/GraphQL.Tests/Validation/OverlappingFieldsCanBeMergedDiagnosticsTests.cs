using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Rules;
using GraphQLParser.AST;

namespace GraphQL.Tests.Validation;

public class OverlappingFieldsCanBeMergedDiagnosticsTests
{
    [Fact]
    public void Nested_conflict_reports_the_actual_fields_and_their_locations()
    {
        var result = Validate("""
            {
                node {
                    child { same: text }
                    child { same: other }
                }
            }
            """);

        var error = result.Errors.ShouldHaveSingleItem();
        error.Message.ShouldBe(Message("same", "text and other are different fields"));
        Locations(error).ShouldBe(["3:17", "4:17"]);
    }

    [Fact]
    public void Independent_later_siblings_and_operations_are_checked_after_an_error()
    {
        var result = Validate("""
            query First {
                first: node { x: text x: other }
                later: node { y: text y: third }
            }
            query Second {
                node { z: text z: other }
            }
            """);

        result.Errors.Count.ShouldBe(3);
        result.Errors.Select(error => error.Message).ShouldBe([
            Message("x", "text and other are different fields"),
            Message("y", "text and third are different fields"),
            Message("z", "text and other are different fields"),
        ]);
    }

    [Fact]
    public void A_parent_conflict_does_not_skip_independent_descendant_validation()
    {
        var result = Validate("""
            {
                node {
                    same: child { nested: text nested: other }
                    same: otherChild { text }
                }
            }
            """);

        result.Errors.ShouldContain(error => error.Message == Message("same", "child and otherChild are different fields"));
        result.Errors.ShouldContain(error => error.Message == Message("nested", "text and other are different fields"));
    }

    [Fact]
    public void An_operation_error_does_not_skip_a_later_unused_fragment()
    {
        var result = Validate("""
            { node { operation: text operation: other } }
            fragment Unused on Node { fragment: text fragment: third }
            """);

        result.Errors.Count.ShouldBe(2);
        result.Errors.Select(error => error.Message).ShouldBe([
            Message("operation", "text and other are different fields"),
            Message("fragment", "text and third are different fields"),
        ]);
    }

    [Fact]
    public void A_type_conflict_uses_known_type_witnesses_after_an_unknown_field()
    {
        var result = Validate("""
            {
                choice {
                    ... on Missing { value }
                    ... on A { value }
                    ... on B { value }
                }
            }
            """);

        var error = result.Errors.ShouldHaveSingleItem();
        error.Message.ShouldBe(Message("value", "they return conflicting types String and Int"));
        Locations(error).ShouldBe(["4:20", "5:20"]);
    }

    [Fact]
    public void A_cached_fragment_summary_keeps_the_known_type_witness()
    {
        var result = Validate("""
            {
                b { value ...Shared }
            }
            fragment Shared on Choice {
                ... on Missing { value }
                ... on A { value }
            }
            """);

        var error = result.Errors.ShouldHaveSingleItem();
        error.Message.ShouldBe(Message("value", "they return conflicting types String and Int"));
        Locations(error).ShouldBe(["6:16", "2:9"]);
    }

    [Fact]
    public void Exclusive_descendants_only_report_incompatible_response_shapes()
    {
        var result = Validate("""
            {
                choice {
                    ... on A { child { same: text } }
                    ... on B { child { same: number } }
                }
            }
            """);

        var error = result.Errors.ShouldHaveSingleItem();
        error.Message.ShouldBe(Message("same", "they return conflicting types String and Int"));
        Locations(error).ShouldBe(["3:28", "4:28"]);
    }

    [Fact]
    public void Exclusive_name_differences_do_not_hide_a_later_overlapping_conflict()
    {
        var result = Validate("""
            {
                choice {
                    ... on A { child { ...Left } }
                    ... on B { child { ...Right } }
                }
                node { ...Left ...Right }
            }
            fragment Left on Node { same: text }
            fragment Right on Node { same: other }
            """);

        var error = result.Errors.ShouldHaveSingleItem();
        error.Message.ShouldBe(Message("same", "text and other are different fields"));
        Locations(error).ShouldBe(["8:25", "9:26"]);
    }

    [Fact]
    public void Reused_conflicting_fragments_do_not_produce_pairwise_error_lists()
    {
        string selections = string.Join(" ", Enumerable.Range(0, 128)
            .Select(i => $"n{i}: node {{ ...Left ...Right ...Left }}"));
        var result = Validate($"{{ {selections} }} fragment Left on Node {{ same: text }} fragment Right on Node {{ same: other }}");

        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBeLessThanOrEqualTo(128);
        foreach (var error in result.Errors)
        {
            error.Message.ShouldBe(Message("same", "text and other are different fields"));
            error.Locations.ShouldNotBeNull().Count.ShouldBe(2);
        }
    }

    [Fact]
    public void Many_invalid_field_arguments_do_not_report_every_possible_pair()
    {
        const int count = 512;
        string selections = string.Join(" ", Enumerable.Range(0, count).Select(i => $"text(format: {i})"));
        var result = Validate($"{{ node {{ {selections} }} }}");

        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBeLessThanOrEqualTo(count - 1);
        foreach (var error in result.Errors)
        {
            error.Message.ShouldBe(Message("text", "they have differing arguments"));
            error.Locations.ShouldNotBeNull().Count.ShouldBe(2);
        }
    }

    private static string Message(string responseName, string reason)
        => $"Fields '{responseName}' conflict because {reason}.";

    private static string[] Locations(ExecutionError error)
        => error.Locations.ShouldNotBeNull().Select(location => $"{location.Line}:{location.Column}").ToArray();

    private static IValidationResult Validate(string query)
    {
        using var schema = Schema.For("""
            type Query { node: Node choice: Choice b: B }
            type Node { text(format: Int): String other: String third: String number: Int child: Node otherChild: Node }
            type A { value: String child: Node }
            type B { value: Int child: Node }
            union Choice = A | B
            """, builder => builder.Types.ForAll(config => config.ResolveType = _ => null!));
        var document = new GraphQLDocumentBuilder().Build(query);
        return new DocumentValidator().ValidateAsync(new ValidationOptions
        {
            Schema = schema,
            Document = document,
            Operation = document.Definitions.OfType<GraphQLOperationDefinition>().First(),
            Rules = [OverlappingFieldsCanBeMerged.Instance],
            Variables = Inputs.Empty,
        }).GetAwaiter().GetResult();
    }
}
