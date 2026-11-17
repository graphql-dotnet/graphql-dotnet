using System.Text;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Rules;
using GraphQLParser.AST;

namespace GraphQL.Tests.Validation;

public class OverlappingFieldsCanBeMergedOptimizationTests
{
    private const string SchemaText = """
        input Filter { a: Int b: String nested: Filter }
        type Query {
            node: Node
            choice: Choice
            named: Named
            otherNamed: OtherNamed
            text(format: Int, input: Filter, values: [Int]): String
            other: String
        }
        type Node {
            text(format: Int, input: Filter, values: [Int]): String
            other: String
            number: Int
            child: Node
            left: Node
            right: Node
            list: [String]
            nn: String!
        }
        interface Named { text(format: Int): String child: Node }
        interface OtherNamed { text(format: Int): String child: Node }
        type A implements Named & OtherNamed {
            text(format: Int): String
            other: String
            child: Node
            value: String
        }
        type B implements Named {
            text(format: Int): String
            other: String
            child: Node
            value: Int
        }
        union Choice = A | B
        """;

    [Theory]
    [InlineData("{ node { text other number child { text } } }")]
    [InlineData("{ node { text text ... { text } } }")]
    [InlineData("{ node { a: text b: text(format: 1) } }")]
    [InlineData("{ node { child { text } child { other } } }")]
    [InlineData("{ node { child { left { text } } child { right { other } } } }")]
    [InlineData("{ node { child { same: text } } alias: node { child { same: other } } }")]
    [InlineData("{ node { text(input: {a: 1, nested: {b: \"x\"}}, format: 2) text(format: 2, input: { a: 1 nested: { b: \"x\" } }) } }")]
    [InlineData("query($f: Int = 1) { node { text(format: $f) text(format: $f) } }")]
    [InlineData("{ node { text @skip(if: true) text @include(if: false) } }")]
    [InlineData("{ node { missing missing } }")]
    [InlineData("{ node { ...Missing text } }")]
    [InlineData("{ choice { ... on A { same: text(format: 1) } ... on B { same: other } } }")]
    [InlineData("{ choice { ... on A { child { same: text(format: 1) } } ... on B { child { same: other } } } }")]
    [InlineData("{ choice { ... on A { child { child { same: text(format: 1) } } } ... on B { child { child { same: text(format: 2) } } } } }")]
    [InlineData("{ named { text(format: 1) ... on A { text(format: 1) } ... on OtherNamed { text(format: 1) } } }")]
    [InlineData("query First { node { same: text } } query Second { node { same: other } }")]
    [InlineData("{ node { text } } fragment Unused on Node { text text }")]
    [InlineData("fragment Unused on Node { child { text } child { other } }")]
    public void Compatible_selections_are_accepted(string query)
    {
        AssertValid(Validate(query));
    }

    [Theory]
    [InlineData("{ node { same: text same: other } }", "different fields")]
    [InlineData("{ node { text text(format: 1) } }", "differing arguments")]
    [InlineData("{ node { text(format: 1) text(format: 2) } }", "differing arguments")]
    [InlineData("{ node { text(values: [1, 2]) text(values: [2, 1]) } }", "differing arguments")]
    [InlineData("{ node { text(input: {nested: {a: 1}}) text(input: {nested: {a: 2}}) } }", "differing arguments")]
    [InlineData("query($a: Int = 1, $b: Int = 1) { node { text(format: $a) text(format: $b) } }", "differing arguments")]
    [InlineData("{ node { text @skip(if: true) text: other @include(if: false) } }", "different fields")]
    [InlineData("{ node { child { text } child { text: other } } }", "different fields")]
    [InlineData("{ node { child { child { text } } child { child { text: other } } } }", "different fields")]
    [InlineData("{ node { child { same: missing } child { same: otherMissing } } }", "different fields")]
    [InlineData("{ choice { ... on A { value } ... on B { value } } }", "conflicting types")]
    [InlineData("{ choice { ... on Missing { value } ... on A { value } ... on B { value } } }", "conflicting types")]
    [InlineData("{ choice { ... on A { child { same: text } } ... on B { child { same: number } } } }", "conflicting types")]
    [InlineData("{ choice { ... on A { child { same: text } } ... on B { child { same: nn } } } }", "conflicting types")]
    [InlineData("{ choice { ... on A { child { same: text } } ... on B { child { same: list } } } }", "conflicting types")]
    [InlineData("{ named { text(format: 1) ... on A { text(format: 2) } } }", "differing arguments")]
    [InlineData("{ named { ... on Named { text(format: 1) } ... on OtherNamed { text(format: 2) } } }", "differing arguments")]
    [InlineData("{ named { ... on Named { child { text } } ... on OtherNamed { child { text: other } } } }", "different fields")]
    [InlineData("{ node { text } } fragment Unused on Node { same: text same: other }", "different fields")]
    [InlineData("query First { node { text } } query Second { node { same: text same: other } }", "different fields")]
    public void Incompatible_selections_are_rejected(string query, string reason)
    {
        AssertConflict(Validate(query), reason);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Deep_cross_fragment_conflicts_do_not_depend_on_definition_or_spread_order(bool reverseDefinitions, bool reverseSpreads)
    {
        const string left = "fragment Left on Node { child { child { same: text } } }";
        const string right = "fragment Right on Node { child { child { same: other } } }";
        string definitions = reverseDefinitions ? right + left : left + right;
        string spreads = reverseSpreads ? "...Right ...Left" : "...Left ...Right";

        var result = Validate($"{{ node {{ {spreads} }} }} {definitions}");

        AssertConflict(result, "different fields");
        result.Errors.Count.ShouldBe(1);
    }

    [Fact]
    public void Repeating_the_same_fragment_preserves_a_cross_fragment_conflict()
    {
        var result = Validate("""
            { node { ...Left ...Left ...Right ...Right } }
            fragment Left on Node { child { same: text } }
            fragment Right on Node { child { same: other } }
            """);

        AssertConflict(result, "different fields");
        result.Errors.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Unused_fragment_merges_do_not_depend_on_prepopulated_child_caches(bool includeOperation, bool reverseDefinitions)
    {
        const string outer = "fragment Outer on Node { child { ...Inner } child { child { same: other } } }";
        const string inner = "fragment Inner on Node { child { same: text } }";
        string definitions = reverseDefinitions ? inner + outer : outer + inner;

        AssertConflict(Validate((includeOperation ? "{ node { text } } " : "") + definitions), "different fields");
    }

    [Fact]
    public void Shared_fragment_is_checked_in_each_distinct_response_path()
    {
        var result = Validate("""
            {
                safe: node { ...Shared child { text } }
                unsafe: node { ...Shared child { text: other } }
            }
            fragment Shared on Node { child { text } }
            """);

        AssertConflict(result, "different fields");
    }

    [Theory]
    [InlineData("fragment Cycle on Node { ...Cycle text text }")]
    [InlineData("fragment Cycle on Node { ...Next text } fragment Next on Node { ...Cycle text }")]
    public void Fragment_cycles_terminate_when_this_rule_runs_alone(string fragments)
    {
        AssertValid(Validate("{ node { ...Cycle } } " + fragments));
    }

    [Fact]
    public void Conflict_is_not_hidden_by_a_fragment_cycle()
    {
        AssertConflict(Validate("""
            { node { ...Cycle } }
            fragment Cycle on Node { ...Next same: text }
            fragment Next on Node { ...Cycle same: other }
            """), "different fields");
    }

    [Fact]
    public void Many_identical_leaf_fields_are_accepted()
    {
        AssertValid(Validate("{ node { " + string.Join(" ", Enumerable.Repeat("text", 2048)) + " } }"));
    }

    [Fact]
    public void Many_distinct_response_names_are_accepted()
    {
        AssertValid(Validate("{ node { " + string.Join(" ", Enumerable.Range(0, 2048).Select(i => $"f{i}: text")) + " } }"));
    }

    [Fact]
    public void Many_nested_inline_fragments_are_accepted()
    {
        string inner = string.Join(" ", Enumerable.Repeat("... on Node { text }", 32));
        string outer = string.Join(" ", Enumerable.Repeat($"... on Node {{ child {{ {inner} }} }}", 64));
        AssertValid(Validate("{ node { " + outer + " } }"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Diamond_fragment_graph_is_not_expanded_per_path(bool reverseDefinitions)
    {
        // There are only 61 definitions, but over a billion routes to the leaf.
        // Repeated spreads must be treated as shared nodes, not expanded as a tree.
        const int depth = 30;
        var definitions = new List<string> { "fragment Leaf on Node { text }" };
        for (int i = 0; i < depth; ++i)
        {
            string children = i == 0 ? "...Leaf ...Leaf" : $"...Left{i - 1} ...Right{i - 1}";
            definitions.Add($"fragment Left{i} on Node {{ {children} }}");
            definitions.Add($"fragment Right{i} on Node {{ {children} }}");
        }

        if (reverseDefinitions)
            definitions.Reverse();

        var query = new StringBuilder($"{{ node {{ ...Left{depth - 1} ...Right{depth - 1} }} }} ");
        foreach (string definition in definitions)
            query.Append(definition).Append(' ');

        AssertValid(Validate(query.ToString()));
    }

    [Fact]
    public void Reusing_a_validator_does_not_reuse_a_previous_documents_conflicts()
    {
        var validator = new DocumentValidator();
        using var schema = CreateSchema();
        AssertValid(Validate("{ node { text text } }", schema, validator));
        AssertConflict(Validate("{ node { text text: other } }", schema, validator), "different fields");
        AssertValid(Validate("{ node { text text } }", schema, validator));
        AssertConflict(Validate("{ node { text text: other } }", schema, validator), "different fields");
    }

    private static IValidationResult Validate(string query)
    {
        using var schema = CreateSchema();
        return Validate(query, schema, new DocumentValidator());
    }

    private static Schema CreateSchema()
        => Schema.For(SchemaText, builder => builder.Types.ForAll(config => config.ResolveType = _ => null!));

    private static IValidationResult Validate(string query, ISchema schema, DocumentValidator validator)
    {
        var document = new GraphQLDocumentBuilder().Build(query);
        return validator.ValidateAsync(new ValidationOptions
        {
            Schema = schema,
            Document = document,
            Operation = document.Definitions.OfType<GraphQLOperationDefinition>().FirstOrDefault()!,
            Rules = [OverlappingFieldsCanBeMerged.Instance],
            Variables = Inputs.Empty,
        }).GetAwaiter().GetResult();
    }

    private static void AssertValid(IValidationResult result)
        => result.IsValid.ShouldBeTrue(string.Join(Environment.NewLine, result.Errors.Select(x => x.Message)));

    private static void AssertConflict(IValidationResult result, string reason)
    {
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.Message.Contains(reason, StringComparison.Ordinal));
        foreach (var error in result.Errors)
            error.Locations.ShouldNotBeNull().Count.ShouldBeGreaterThanOrEqualTo(2);
    }
}
