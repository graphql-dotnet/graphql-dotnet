using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;
using GraphQLParser.AST;

namespace GraphQL.Tests.Validation;

public class OverlappingFieldsCanBeMergedSpecTests
{
    private const string ArgumentsSchema = """
        input Pair { a: Int b: Int nested: Pair items: [Pair] text: String }
        enum Tone { RED BLUE }
        type Query {
            field(input: Pair, second: Int, value: Float, values: [Int], text: String, tone: Tone, flag: Boolean): String
            other: String
        }
        """;

    public static IEnumerable<object[]> WrappedParents()
    {
        foreach (string wrapper in new[] { "T", "T!", "[T]", "[T!]!", "[[T!]!]!" })
        {
            foreach (string parent in new[] { "Named", "A" })
            {
                foreach (bool namedFragment in new[] { false, true })
                    yield return [wrapper.Replace("T", parent), parent, namedFragment];
            }
        }
    }

    [Theory]
    [MemberData(nameof(WrappedParents))]
    public async Task Parent_wrappers_do_not_hide_covariant_leaf_type_conflicts(string parentType, string parentName, bool namedFragment)
    {
        string schema = "type Query { node: " + parentType + " a: A }" + """
            interface Named { value: String }
            type A implements Named { value: String! }
            """;
        string otherParent = parentName == "Named" ? "A" : "Named";
        string query = namedFragment
            ? $"{{ node {{ value ...F }} }} fragment F on {otherParent} {{ value }}"
            : $"{{ node {{ value ... on {otherParent} {{ value }} }} }}";

        await AssertValidationAsync(schema, query, false);

        // The same underlying parent still has the same shape, whatever the wrapper.
        string compatible = namedFragment
            ? $"{{ node {{ value ...F }} }} fragment F on {parentName} {{ value }}"
            : $"{{ node {{ value ... on {parentName} {{ value }} }} }}";
        await AssertValidationAsync(schema, compatible, true);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task An_additional_parent_selection_does_not_change_shape_validation(bool duplicateParent, bool namedFragment)
    {
        const string schema = """
            type Query { node: Named! a: A }
            interface Named { value: String }
            type A implements Named { value: String! }
            """;
        string selection = namedFragment ? "value ...F" : "value ... on A { value }";
        string query = "{ node { " + selection + " } " + (duplicateParent ? "node { __typename }" : "") + " }";
        if (namedFragment)
            query += " fragment F on A { value }";

        await AssertValidationAsync(schema, query, false);
    }

    public static IEnumerable<object[]> TypenameCases()
    {
        foreach (string form in new[] { "inline", "named", "mixed", "nested", "cached" })
        {
            foreach (string field in new[] { "count", "nullableText", "text", "values" })
                yield return [form, field, field == "text"];
        }
    }

    [Theory]
    [MemberData(nameof(TypenameCases))]
    public async Task Typename_is_compared_as_non_null_String(string form, string field, bool valid)
    {
        const string schema = """
            type Query { choice: Choice }
            union Choice = A | B
            type A { value: String! }
            type B { count: Int! nullableText: String text: String! values: [String!]! }
            """;
        string query = form switch
        {
            "inline" => $"{{ choice {{ ... on A {{ same: __typename }} ... on B {{ same: {field} }} }} }}",
            "named" => $"{{ choice {{ ...Left ...Right }} }} fragment Left on A {{ same: __typename }} fragment Right on B {{ same: {field} }}",
            "mixed" => $"{{ choice {{ __typename ...Left ...Right }} }} fragment Left on A {{ same: __typename }} fragment Right on B {{ same: {field} }}",
            "nested" => $"{{ choice {{ ... on A {{ same: __typename }} }} choice {{ ... on B {{ same: {field} }} }} }}",
            _ => $"{{ first: choice {{ __typename ...Left ...Right }} second: choice {{ __typename ...Right ...Left }} }} fragment Left on A {{ same: __typename }} fragment Right on B {{ same: {field} }}",
        };

        await AssertValidationAsync(schema, query, valid);
    }

    [Theory]
    [InlineData("__type(name: \"B\") { name }")]
    [InlineData("__schema { queryType { name } }")]
    public async Task Introspection_meta_fields_have_composite_response_shapes(string selection)
    {
        const string schema = "type Query { choice: Choice } union Choice = Query | B type B { count: Int! }";
        string query = $"{{ choice {{ ... on Query {{ same: {selection} }} ... on B {{ same: count }} }} }}";

        await AssertValidationAsync(schema, query, false);
    }

    [Theory]
    [InlineData("{ __type(name: \"Query\") { name } __type(name: \"Query\") { kind } }")]
    [InlineData("{ __schema { queryType { name } } __schema { queryType { kind } } }")]
    public async Task Repeated_introspection_meta_fields_merge_compatible_subselections(string query)
    {
        await AssertValidationAsync("type Query { value: String }", query, true);
    }

    [Theory]
    [InlineData("{ field(input: {a: 1, b: 2}) field(input: {b: 2, a: 1}) }")]
    [InlineData("{ field(input: {nested: {a: 1, b: 2}}) field(input: {nested: {b: 2, a: 1}}) }")]
    [InlineData("{ field(input: {items: [{a: 1, b: 2}]}) field(input: {items: [{b: 2, a: 1}]}) }")]
    [InlineData("{ field(input: {items: [{a: 1, b: 2}, {a: 3, b: 4}]}) field(input: {items: [{b: 2, a: 1}, {b: 4, a: 3}]}) }")]
    [InlineData("{ field(input: {a: 1, items: [{a: 2, nested: {a: 3, b: 4}}]}) field(input: {items: [{nested: {b: 4, a: 3}, a: 2}], a: 1}) }")]
    [InlineData("query($x: Int = 1, $y: Int = 2) { field(input: {a: $x, b: $y}) field(input: {b: $y, a: $x}) }")]
    public async Task Input_object_field_order_does_not_change_argument_identity(string query)
    {
        await AssertValidationAsync(ArgumentsSchema, query, true);
    }

    [Theory]
    [InlineData("{ field(second: 1, value: 2.0) field(value: 2.0, second: 1) }", true)]
    [InlineData("{ field(values: [1, 2]) field(values: [2, 1]) }", false)]
    [InlineData("{ field(input: {a: 1, b: 2}) field(input: {a: 1, b: 3}) }", false)]
    [InlineData("{ field(input: {a: 1, b: 2}) field(input: {a: 1}) }", false)]
    [InlineData("{ field(input: {items: [{a: 1}, {a: 2}]}) field(input: {items: [{a: 2}, {a: 1}]}) }", false)]
    [InlineData("{ field(input: {}) field(input: {}) }", true)]
    [InlineData("{ field(values: []) field(values: []) }", true)]
    [InlineData("{ field(values: []) field(values: [1]) }", false)]
    [InlineData("query($x: Float = 1) { field(value: $x) field(value: $x) }", true)]
    [InlineData("query($x: Float = 1, $y: Float = 1) { field(value: $x) field(value: $y) }", false)]
    [InlineData("query($x: Float = 1) { field(value: $x) field(value: 1) }", false)]
    [InlineData("{ field(value: null) field }", false)]
    [InlineData("{ field(value: null) field(value: null) }", true)]
    [InlineData("{ field(value: 1) field(value: 1.0) }", false)]
    [InlineData("{ field(value: 1.0) field(value: 1.00) }", false)]
    [InlineData("{ field(value: 1.0) field(value: 1e0) }", false)]
    [InlineData("{ field(text: \"a\") field(text: \"\\u0061\") }", true)]
    [InlineData("{ field(tone: RED) field(tone: RED) }", true)]
    [InlineData("{ field(tone: RED) field(tone: BLUE) }", false)]
    [InlineData("{ field(flag: true) field(flag: true) }", true)]
    [InlineData("{ field(flag: true) field(flag: false) }", false)]
    [InlineData("{ field @include(if: true) field @include(if: false) }", true)]
    [InlineData("{ same: field @include(if: true) same: other @include(if: false) }", false)]
    [InlineData("{ field(values: 1) field(values: [1]) }", false)]
    public async Task Argument_identity_preserves_literal_list_variable_and_directive_semantics(string query, bool valid)
    {
        await AssertValidationAsync(ArgumentsSchema, query, valid);
    }

    [Theory]
    [InlineData(64, 64, false)]
    [InlineData(64, 0, false)]
    [InlineData(16, 8, false)]
    [InlineData(16, 7, false)]
    [InlineData(16, 8, true)]
    [InlineData(16, 7, true)]
    public async Task Large_argument_sets_compare_ordered_prefixes_and_reordered_suffixes(int count, int prefix, bool differentValue)
    {
        string definitions = string.Join(", ", Enumerable.Range(0, count).Select(i => $"a{i}: Int"));
        string schema = $"type Query {{ field({definitions}): String }}";
        string left = string.Join(", ", Enumerable.Range(0, count).Select(i => $"a{i}: {i}"));
        var order = Enumerable.Range(0, prefix).Concat(Enumerable.Range(prefix, count - prefix).Reverse());
        string right = string.Join(", ", order.Select(i => $"a{i}: {(differentValue && i == count - 1 ? -1 : i)}"));

        await AssertValidationAsync(schema, $"{{ field({left}) field({right}) }}", !differentValue);
    }

    [Theory]
    [InlineData(64, 64, false)]
    [InlineData(64, 0, false)]
    [InlineData(16, 8, false)]
    [InlineData(16, 7, false)]
    [InlineData(16, 8, true)]
    [InlineData(16, 7, true)]
    public async Task Large_nested_object_values_compare_ordered_prefixes_and_reordered_suffixes(int count, int prefix, bool differentValue)
    {
        string fields = string.Join(" ", Enumerable.Range(0, count).Select(i => $"a{i}: Int"));
        string schema = $"input Many {{ {fields} }} input Container {{ nested: Many items: [Many] }} type Query {{ field(input: Container): String }}";
        string left = string.Join(", ", Enumerable.Range(0, count).Select(i => $"a{i}: {i}"));
        var order = Enumerable.Range(0, prefix).Concat(Enumerable.Range(prefix, count - prefix).Reverse());
        string right = string.Join(", ", order.Select(i => $"a{i}: {(differentValue && i == count - 1 ? -1 : i)}"));
        string query = $"{{ field(input: {{nested: {{{left}}}, items: [{{{left}}}, {{a0: -2}}]}}) field(input: {{items: [{{{right}}}, {{a0: -2}}], nested: {{{right}}}}}) }}";

        await AssertValidationAsync(schema, query, !differentValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Duplicate_argument_or_object_names_are_rejected_without_throwing(bool objectFields)
    {
        const int count = 16;
        string definitions = string.Join(" ", Enumerable.Range(0, count).Select(i => $"a{i}: Int"));
        string left = string.Join(", ", Enumerable.Range(0, count).Select(i => $"a{i}: {i}"));
        string right = string.Join(", ", Enumerable.Range(0, count).Reverse().Select(i => $"a{(i == 0 ? 1 : i)}: {i}"));
        string schema = objectFields
            ? $"input Many {{ {definitions} }} type Query {{ field(input: Many): String }}"
            : $"type Query {{ field({definitions}): String }}";
        string query = objectFields
            ? $"{{ field(input: {{{left}}}) field(input: {{{right}}}) }}"
            : $"{{ field({left}) field({right}) }}";

        using var graph = CreateSchema(schema);
        var result = await ValidateAsync(graph, query, allRules: true);
        result.IsValid.ShouldBeFalse();
        if (objectFields)
            result.Errors.ShouldContain(error => error is UniqueInputFieldNamesError);
        else
            result.Errors.ShouldContain(error => error is UniqueArgumentNamesError);
    }

    private static async Task AssertValidationAsync(string schema, string query, bool valid)
    {
        using var graph = CreateSchema(schema);
        foreach (bool allRules in new[] { false, true })
        {
            var result = await ValidateAsync(graph, query, allRules);
            result.IsValid.ShouldBe(valid, $"allRules={allRules}\n{query}\n{string.Join(Environment.NewLine, result.Errors.Select(error => error.Message))}");
            if (!valid)
                result.Errors.ShouldAllBe(error => error is OverlappingFieldsCanBeMergedError);
        }
    }

    private static Schema CreateSchema(string schema)
        => Schema.For(schema, builder => builder.Types.ForAll(config => config.ResolveType = _ => null!));

    private static Task<IValidationResult> ValidateAsync(ISchema schema, string query, bool allRules)
    {
        var document = new GraphQLDocumentBuilder().Build(query);
        return new DocumentValidator().ValidateAsync(new ValidationOptions
        {
            Schema = schema,
            Document = document,
            Operation = document.Definitions.OfType<GraphQLOperationDefinition>().First(),
            Rules = allRules ? DocumentValidator.CoreRules : [OverlappingFieldsCanBeMerged.Instance],
            Variables = Inputs.Empty,
        });
    }
}
