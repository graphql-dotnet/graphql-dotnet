using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Rules;
using GraphQLParser.AST;

namespace GraphQL.Tests.Validation;

public class OverlappingFieldsCanBeMergedFragmentReuseTests
{
    [Fact]
    public void Reused_fragment_groups_allow_reordered_and_duplicate_spreads()
    {
        var selections = string.Join(" ", Enumerable.Range(0, 128)
            .Select(i => $"a{i}: node {{ ...Left ...Right ...Left ...Missing }} b{i}: node {{ ...Missing ...Right ...Left }}"));
        var left = string.Join(" ", Enumerable.Range(0, 128).Select(i => $"l{i}: text"));
        var right = string.Join(" ", Enumerable.Range(0, 128).Select(i => $"r{i}: text"));

        Validate($"{{ {selections} }} fragment Left on Node {{ {left} }} fragment Right on Node {{ {right} }}")
            .IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Individually_checked_fragments_can_still_conflict_when_combined()
    {
        AssertConflict("""
            {
                first: node { ...Left }
                second: node { ...Right }
                third: node { ...Left ...Right }
            }
            fragment Left on Node { same: text }
            fragment Right on Node { same: other }
            """);
    }

    [Fact]
    public void A_checked_group_does_not_hide_a_conflict_in_a_different_group()
    {
        AssertConflict("""
            {
                first: node { ...Left ...Compatible }
                second: node { ...Compatible ...Left ...Compatible }
                third: node { ...Left ...Conflicting }
            }
            fragment Left on Node { same: text }
            fragment Compatible on Node { same: text }
            fragment Conflicting on Node { same: other }
            """);
    }

    [Fact]
    public void Reused_fragment_roots_do_not_skip_invalid_descendant_selection_sets()
    {
        AssertConflict("""
            {
                first: node { ...Outer }
                second: node { ...Outer ...Outer }
            }
            fragment Outer on Node { child { same: text same: other } }
            """);
    }

    [Fact]
    public void Shape_checks_across_exclusive_parents_do_not_cache_name_compatibility()
    {
        AssertConflict("""
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
    }

    [Fact]
    public void Fragment_group_reuse_terminates_cycles_and_ignores_missing_fragments()
    {
        Validate("""
            {
                first: node { ...Left ...Right ...Missing }
                second: node { ...Missing ...Right ...Left }
            }
            fragment Left on Node { text ...Right }
            fragment Right on Node { text ...Left }
            """).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Mixed_leaf_selections_reuse_large_fragment_groups()
    {
        var selections = string.Join(" ", Enumerable.Range(0, 128)
            .Select(i => $"a{i}: node {{ text ...Left ...Right ...Missing text }} b{i}: node {{ ...Right local: text ...Left }}"));
        var left = string.Join(" ", Enumerable.Range(0, 128).Select(i => $"l{i}: text"));
        var right = string.Join(" ", Enumerable.Range(0, 128).Select(i => $"r{i}: text"));

        Validate($"{{ {selections} }} fragment Left on Node {{ {left} }} fragment Right on Node {{ {right} }}")
            .IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("{ node { ...F same: other } } fragment F on Node { same: text }", "different fields")]
    [InlineData("{ node { ...F text(format: 2) } } fragment F on Node { text(format: 1) }", "differing arguments")]
    [InlineData("{ node { ...F same: text same: other } } fragment F on Node { different: text }", "different fields")]
    [InlineData("{ node { ...F text(format: 1) text(format: 2) } } fragment F on Node { other }", "differing arguments")]
    [InlineData("{ a { ...F same: text } } fragment F on B { same: number }", "conflicting types")]
    [InlineData("{ node { text ...F } } fragment F on Node { child { same: text same: other } }", "different fields")]
    [InlineData("{ node { text ...F ...G } } fragment F on Node { child { same: text } } fragment G on Node { child { same: other } }", "different fields")]
    public void Mixed_leaf_summaries_preserve_conflicts(string query, string reason)
    {
        var result = Validate(query);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.Message.Contains(reason, StringComparison.Ordinal));
    }

    [Fact]
    public void Mixed_leaf_summary_uses_actual_local_parent_without_mutating_cached_representatives()
    {
        Validate("""
            {
                a { same: text ...Shared }
                b { same: other ...Shared }
                again: a { same: text ...Shared }
            }
            fragment Shared on Choice {
                ... on A { same: text }
                ... on B { same: other }
            }
            """).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Abstract_local_leaf_checks_every_concrete_summary_representative()
    {
        AssertConflict("""
            {
                a { same: text ...Shared }
                b { same: other ...Shared }
                named { same: text ...Shared }
            }
            fragment Shared on Choice {
                ... on A { same: text }
                ... on B { same: other }
            }
            """);
    }

    [Fact]
    public void Concrete_local_leaf_checks_abstract_summary_representative()
    {
        AssertConflict("""
            {
                a { same: text ...Shared }
                b { same: other ...Shared }
            }
            fragment Shared on Named { same: text }
            """);
    }

    [Fact]
    public void Mixed_leaf_summary_compares_types_when_first_fragment_field_is_unknown()
    {
        var result = Validate("""
            { b { value ...Shared } }
            fragment Shared on Choice {
                ... on Missing { value }
                ... on A { value }
            }
            """);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.Message.Contains("conflicting types", StringComparison.Ordinal));
    }

    [Fact]
    public void Mixed_leaf_summary_does_not_require_subselection_for_a_local_composite_field()
    {
        // ScalarLeafs owns the missing-selection error. This rule must still
        // compare the field with fragment selections without adding child state.
        Validate("""
            { node { child ...Shared } }
            fragment Shared on Node { child { text } }
            """).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Mixed_leaf_summary_terminates_recursive_and_missing_fragments()
    {
        Validate("""
            {
                first: node { text ...Left ...Right ...Missing }
                second: node { text ...Missing ...Right ...Left }
            }
            fragment Left on Node { text ...Right }
            fragment Right on Node { text ...Left }
            """).IsValid.ShouldBeTrue();
    }

    private static void AssertConflict(string query)
    {
        var result = Validate(query);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.Message.Contains("different fields", StringComparison.Ordinal));
    }

    private static IValidationResult Validate(string query)
    {
        using var schema = Schema.For("""
            type Query { node: Node choice: Choice a: A b: B named: Named }
            type Node { text(format: Int): String other: String number: Int child: Node }
            interface Named { text: String child: Node }
            type A implements Named { text: String other: String number: Int value: String child: Node }
            type B implements Named { text: String other: String number: Int value: Int child: Node }
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
