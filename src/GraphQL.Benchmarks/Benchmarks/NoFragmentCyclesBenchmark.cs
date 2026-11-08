using System.Text;
using BenchmarkDotNet.Attributes;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Rules;

namespace GraphQL.Benchmarks;

[MemoryDiagnoser]
public class NoFragmentCyclesBenchmark : IBenchmark
{
    private ValidationOptions _options;

    [Params("Typical", "Chain256", "PendingSiblings257", "Wide128", "Diamonds128", "Cycle64")]
    public string Scenario { get; set; } = "Typical";

    [GlobalSetup]
    public void GlobalSetup()
    {
        var query = Scenario switch
        {
            "Typical" => """
                fragment A on Node { id ...B ...C }
                fragment B on Node { id ...D }
                fragment C on Node { id ...D }
                fragment D on Node { id }
                """,
            "Chain256" => BuildChain(256),
            "PendingSiblings257" => BuildChain(259, siblings: true),
            "Wide128" => "fragment Root on Node { " + string.Join(" ", Enumerable.Range(0, 128).Select(i => $"...F{i}")) + " } "
                + string.Join(" ", Enumerable.Range(0, 128).Select(i => $"fragment F{i} on Node {{ ...Leaf }}"))
                + " fragment Leaf on Node { id }",
            "Diamonds128" => BuildDiamonds(128),
            "Cycle64" => BuildChain(64, cycle: true),
            _ => throw new InvalidOperationException($"Unknown scenario: {Scenario}"),
        };
        var schema = Schema.For("type Query { root: Node } type Node { id: ID }");
        schema.Initialize();
        // Fragment-only documents isolate cycle detection from the recursive
        // fragment collectors and argument parsing used for an operation.
        _options = new ValidationOptions(
            schema: schema,
            document: new GraphQLDocumentBuilder().Build(query),
            rules: [NoFragmentCycles.Instance],
            userContext: null,
            metrics: null,
            variables: null,
            extensions: null,
            operation: null,
            requestServices: null,
            user: null,
            cancellationToken: default);
        if (Validate().Errors.Count() != (Scenario == "Cycle64" ? 1 : 0))
            throw new InvalidOperationException("Unexpected validation result.");
    }

    // Parsing, schema initialization and options are prepared outside measurement.
    // A new validator also gives each invocation a fresh validation context.
    [Benchmark]
    public IValidationResult Validate() => new DocumentValidator().ValidateAsync(_options).GetAwaiter().GetResult();

    private static string BuildChain(int count, bool siblings = false, bool cycle = false)
    {
        var query = new StringBuilder();
        for (int i = 0; i < count; ++i)
        {
            query.Append("fragment F").Append(i).Append(" on Node { ");
            if (i + 1 < count)
            {
                query.Append("...F").Append(i + 1);
                if (siblings)
                    query.Append(" ...Leaf");
            }
            else
            {
                query.Append(cycle ? "...F0" : "id");
            }
            query.Append(" } ");
        }
        if (siblings)
            query.Append("fragment Leaf on Node { id }");
        return query.ToString();
    }

    private static string BuildDiamonds(int count)
    {
        var query = new StringBuilder();
        for (int i = 0; i < count; ++i)
        {
            query.Append("fragment A").Append(i).Append(" on Node { ...B").Append(i).Append(" ...C").Append(i).Append(" } ");
            query.Append("fragment B").Append(i).Append(" on Node { ...A").Append(i + 1).Append(" } ");
            query.Append("fragment C").Append(i).Append(" on Node { ...A").Append(i + 1).Append(" } ");
        }
        return query.Append("fragment A").Append(count).Append(" on Node { id }").ToString();
    }

    void IBenchmark.RunProfiler() => Validate();
}
