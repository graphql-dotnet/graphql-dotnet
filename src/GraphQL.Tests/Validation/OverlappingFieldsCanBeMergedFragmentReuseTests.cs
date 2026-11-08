using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Rules;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Tests.Validation;

public class OverlappingFieldsCanBeMergedFragmentReuseTests
{
    [Theory]
    [InlineData("text")]
    [InlineData("text other text")]
    [InlineData("same: text same: text")]
    [InlineData("child { same: text same: text }")]
    [InlineData("child { text } child { text }")]
    public void Later_spreads_do_not_repeat_field_lookups_for_a_checked_definition(string selections)
    {
        var definition = $"fragment Shared on Node {{ {selections} }}";
        var baseline = ValidateWithFieldLookups($"{definition} {{ __typename }}");
        var uses = string.Join(" ", Enumerable.Range(0, 128)
            .Select(i => $"n{i}: node {{ ...Shared ...Shared }}"));
        var reused = ValidateWithFieldLookups($"{definition} {{ {uses} }}");

        baseline.Result.IsValid.ShouldBeTrue();
        reused.Result.IsValid.ShouldBeTrue();
        baseline.FieldLookups.ShouldBeGreaterThan(0);
        // TypeInfo visits the definition's fields in both documents. Additional
        // lookups on Node would mean a spread expanded the already checked root.
        reused.FieldLookups.ShouldBe(baseline.FieldLookups);
    }

    [Fact]
    public void A_failed_definition_is_checked_again_when_spread_later()
    {
        const string definition = "fragment Invalid on Node { same: text same: other }";
        var baseline = ValidateWithFieldLookups($"{definition} {{ __typename }}");
        var reused = ValidateWithFieldLookups($"{definition} {{ node {{ ...Invalid }} }}");

        baseline.Result.IsValid.ShouldBeFalse();
        reused.Result.IsValid.ShouldBeFalse();
        reused.FieldLookups.ShouldBeGreaterThan(baseline.FieldLookups);
        reused.Result.Errors.ShouldContain(error => error.Message.Contains("different fields", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void Forward_references_are_resolved_regardless_of_definition_order(bool definitionsFirst, bool conflict)
    {
        var definitions = $$"""
            fragment First on Node { ...Last same: text }
            fragment Last on Node { same: {{(conflict ? "other" : "text")}} }
            """;
        const string operation = "{ first: node { ...First ...First } second: node { ...Last ...First same: text } }";

        var result = Validate(InDefinitionOrder(definitions, operation, definitionsFirst));

        result.IsValid.ShouldBe(!conflict);
        if (conflict)
            result.Errors.ShouldContain(error => error.Message.Contains("different fields", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_trivial_definition_root_does_not_skip_invalid_nested_children(bool definitionsFirst)
    {
        const string definitions = "fragment Outer on Node { child { child { same: text same: other } } }";
        const string operation = "{ first: node { ...Outer } second: node { ...Outer ...Outer } }";

        AssertConflict(InDefinitionOrder(definitions, operation, definitionsFirst));
    }

    public static IEnumerable<object[]> DefinitionOrderConflicts()
    {
        foreach (bool definitionsFirst in new[] { false, true })
        {
            foreach (bool localLeaf in new[] { false, true })
            {
                yield return [definitionsFirst, localLeaf, "same: text", "same: other", "different fields"];
                yield return [definitionsFirst, localLeaf, "text(format: 1)", "text(format: 2)", "differing arguments"];
                yield return [definitionsFirst, localLeaf, "same: text", "same: number", "conflicting types"];
            }
        }
    }

    [Theory]
    [MemberData(nameof(DefinitionOrderConflicts))]
    public void Checked_definitions_still_conflict_when_combined(
        bool definitionsFirst, bool localLeaf, string left, string right, string reason)
    {
        var definitions = $"fragment Left on Node {{ {left} }} fragment Right on Node {{ {right} }}";
        var combined = localLeaf ? $"...Left {right}" : "...Left ...Right";
        var operation = $$"""
            {
                first: node { ...Left }
                second: node { ...Right }
                combined: node { {{combined}} }
            }
            """;

        var result = Validate(InDefinitionOrder(definitions, operation, definitionsFirst));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.Message.Contains(reason, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Checked_definitions_preserve_names_after_exclusive_shape_checks(bool definitionsFirst)
    {
        const string definitions = "fragment Left on Node { same: text } fragment Right on Node { same: other }";
        const string exclusiveOperation = """
            query Exclusive {
                choice {
                    ... on A { child { ...Left } }
                    ... on B { child { ...Right } }
                }
            }
            """;

        Validate(InDefinitionOrder(definitions, exclusiveOperation, definitionsFirst)).IsValid.ShouldBeTrue();
        AssertConflict(InDefinitionOrder(definitions,
            exclusiveOperation + " query Together { node { ...Left ...Right } }", definitionsFirst));
    }

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

    private static string InDefinitionOrder(string definitions, string operation, bool definitionsFirst)
        => definitionsFirst ? $"{definitions}\n{operation}" : $"{operation}\n{definitions}";

    private static (IValidationResult Result, int FieldLookups) ValidateWithFieldLookups(string query)
    {
        var node = new CountingNodeGraphType();
        var root = new ObjectGraphType { Name = "Query" };
        root.AddField(new FieldType { Name = "node", ResolvedType = node });
        using var schema = new Schema { Query = root };
        schema.Initialize();
        node.FieldLookups = 0;

        var result = Validate(query, schema);
        return (result, node.FieldLookups);
    }

    private sealed class CountingNodeGraphType : ObjectGraphType, IComplexGraphType
    {
        public CountingNodeGraphType()
        {
            Name = "Node";
            Field<StringGraphType>("text");
            Field<StringGraphType>("other");
            AddField(new FieldType { Name = "child", ResolvedType = this });
        }

        public int FieldLookups { get; set; }

        FieldType? IComplexGraphType.GetField(ROM name)
        {
            ++FieldLookups;
            return GetField(name);
        }
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
        return Validate(query, schema);
    }

    private static IValidationResult Validate(string query, ISchema schema)
    {
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
