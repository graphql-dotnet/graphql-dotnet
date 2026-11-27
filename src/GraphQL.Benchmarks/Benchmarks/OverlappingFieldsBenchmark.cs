using BenchmarkDotNet.Attributes;
using GraphQL.Execution;
using GraphQL.StarWars;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using GraphQL.Types.Relay;
using GraphQL.Validation;
using GraphQL.Validation.Rules;
using GraphQLParser.AST;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Benchmarks;

/// <summary>
/// Measures field-overlap validation for ordinary queries and adversarial selection
/// patterns. Parsing and schema initialization are outside measurement.
/// </summary>
[MemoryDiagnoser]
public class OverlappingFieldsBenchmark : IBenchmark
{
    public enum QueryCase
    {
        Hero,
        Fragments,
        Introspection,
        UniqueAliases1000,
        RepeatedLeaf100,
        RepeatedLeaf1000,
        UnknownFirst100,
        UnknownFirst1000,
        LateConflict100,
        LateConflict1000,
        ManyConflicts100,
        InlineFragments10x10,
        InlineFragments100x50,
        FragmentDiamond8,
        FragmentDiamond12,
        RepeatedSpread1000,
        RepeatedComposite1000,
        SharedFragments100,
        SharedFragments1000,
        SharedMixed100,
        SharedMixed1000,
        ScalarArguments100,
        NestedObjectArguments100,
        ReorderedObjectArguments100,
    }

    [ParamsAllValues]
    public QueryCase Query { get; set; }

    private ISchema _schema;
    private GraphQLDocument _document;
    private GraphQLOperationDefinition _operation;
    private readonly DocumentValidator _validator = new();
    private IValidationRule[] _rules;
    private ServiceProvider _provider;

    [GlobalSetup]
    public void GlobalSetup()
    {
        string query;
        bool allRules = Query is QueryCase.Hero or QueryCase.Fragments or QueryCase.Introspection;
        if (allRules)
        {
            var services = new ServiceCollection();
            services.AddSingleton<StarWarsData>();
            services.AddSingleton<StarWarsQuery>();
            services.AddSingleton<StarWarsMutation>();
            services.AddSingleton<HumanType>();
            services.AddSingleton<HumanInputType>();
            services.AddSingleton<DroidType>();
            services.AddSingleton<CharacterInterface>();
            services.AddSingleton<EpisodeEnum>();
            services.AddSingleton<ISchema, StarWarsSchema>();
            services.AddSingleton<ConnectionType<CharacterInterface, EdgeType<CharacterInterface>>>();
            services.AddSingleton<EdgeType<CharacterInterface>>();
            services.AddSingleton<PageInfoType>();
            _provider = services.BuildServiceProvider();
            _schema = _provider.GetRequiredService<ISchema>();
            query = Query switch
            {
                QueryCase.Hero => Queries.Hero,
                QueryCase.Fragments => Queries.Fragments,
                _ => Queries.Introspection,
            };
        }
        else
        {
            _schema = Schema.For("""
                type Query { field: Node }
                type Node { f: Node, g: Node, x: String, argument(value: Int, input: ArgumentInput): String }
                input ArgumentInput {
                  a: Int b: Int c: Int d: Int e: Int f: Int g: Int h: Int i: Int j: Int
                  nested: ArgumentInput items: [ArgumentInput]
                }
                """);
            query = Query switch
            {
                QueryCase.UniqueAliases1000 => "{ field { " + string.Join(" ", Enumerable.Range(0, 1000).Select(i => $"x{i}: x")) + " } }",
                QueryCase.RepeatedLeaf100 => "{ field { " + Repeat("x", 100) + " } }",
                QueryCase.RepeatedLeaf1000 => "{ field { " + Repeat("x", 1000) + " } }",
                QueryCase.UnknownFirst100 => "{ field { ... on Missing { x } " + Repeat("x", 100) + " } }",
                QueryCase.UnknownFirst1000 => "{ field { ... on Missing { x } " + Repeat("x", 1000) + " } }",
                QueryCase.LateConflict100 => "{ field { " + Repeat("same: x", 100) + " same: g } }",
                QueryCase.LateConflict1000 => "{ field { " + Repeat("same: x", 1000) + " same: g } }",
                QueryCase.ManyConflicts100 => "{ field { " + Repeat("same: x same: g", 50) + " } }",
                QueryCase.InlineFragments10x10 => InlineFragments(10, 10),
                QueryCase.InlineFragments100x50 => InlineFragments(100, 50),
                QueryCase.FragmentDiamond8 => FragmentDiamond(8),
                QueryCase.FragmentDiamond12 => FragmentDiamond(12),
                QueryCase.RepeatedSpread1000 => "{ field { " + Repeat("...F", 1000) + " } } fragment F on Node { x }",
                QueryCase.RepeatedComposite1000 => "{ field { " + Repeat("f { x }", 1000) + " } }",
                QueryCase.SharedFragments100 => SharedFragments(100),
                QueryCase.SharedFragments1000 => SharedFragments(1000),
                QueryCase.SharedMixed100 => SharedFragments(100, true),
                QueryCase.SharedMixed1000 => SharedFragments(1000, true),
                QueryCase.ScalarArguments100 => "{ field { " + Repeat("argument(value: 1)", 100) + " } }",
                QueryCase.NestedObjectArguments100 => "{ field { " + Repeat(ObjectArgument(false), 100) + " } }",
                QueryCase.ReorderedObjectArguments100 => "{ field { " + Repeat(ObjectArgument(false) + " " + ObjectArgument(true), 50) + " } }",
                _ => throw new InvalidOperationException(),
            };
        }

        _schema.Initialize();
        _document = new GraphQLDocumentBuilder().Build(query);
        _operation = _document.Definitions.OfType<GraphQLOperationDefinition>().First();
        _rules = allRules ? DocumentValidator.CoreRules.ToArray() : [OverlappingFieldsCanBeMerged.Instance];

        // Keep invalid fixtures bounded while exercising sparse and dense conflicts.
        var result = Validate();
        bool expectedValid = Query is not (QueryCase.LateConflict100 or QueryCase.LateConflict1000 or QueryCase.ManyConflicts100);
        if (result.IsValid != expectedValid)
            throw new InvalidOperationException($"Unexpected validation result for {Query}: {string.Join("; ", result.Errors.Select(e => e.Message))}");
    }

    [GlobalCleanup]
    public void Cleanup() => _provider?.Dispose();

    [Benchmark]
    public IValidationResult Validate() =>
        _validator.ValidateAsync(new ValidationOptions
        {
            Schema = _schema,
            Document = _document,
            Operation = _operation,
            Rules = _rules,
        }).GetAwaiter().GetResult();

    void IBenchmark.RunProfiler() => _ = Validate();

    private static string Repeat(string selection, int count) => string.Join(" ", Enumerable.Repeat(selection, count));

    private static string ObjectArgument(bool reordered) => reordered
        ? "argument(input: {items: [{b: 2 a: 1} {d: 4 c: 3}] nested: {b: 2 a: 1} j: 10 i: 9 h: 8 g: 7 f: 6 e: 5 d: 4 c: 3 b: 2 a: 1})"
        : "argument(input: {a: 1 b: 2 c: 3 d: 4 e: 5 f: 6 g: 7 h: 8 i: 9 j: 10 nested: {a: 1 b: 2} items: [{a: 1 b: 2} {c: 3 d: 4}]})";

    private static string InlineFragments(int outer, int inner) =>
        "{ field { " + Repeat("... on Node { f { " + Repeat("... on Node { x }", inner) + " } }", outer) + " } }";

    private static string FragmentDiamond(int depth)
    {
        var query = new System.Text.StringBuilder("{ field { ...F0 } } ");
        for (int i = 0; i < depth; ++i)
            query.Append($"fragment F{i} on Node {{ ...F{i + 1} ...F{i + 1} }} ");
        return query.Append($"fragment F{depth} on Node {{ x }}").ToString();
    }

    private static string SharedFragments(int aliases, bool localField = false) =>
        "{ field { " + string.Join(" ", Enumerable.Range(0, aliases).Select(i => $"a{i}: f {{ {(localField ? "x " : "")}...F ...G }}")) + " } } " +
        "fragment F on Node { " + string.Join(" ", Enumerable.Range(0, 50).Select(i => $"f{i}: x")) + " } " +
        "fragment G on Node { " + string.Join(" ", Enumerable.Range(0, 50).Select(i => $"g{i}: x")) + " }";
}
