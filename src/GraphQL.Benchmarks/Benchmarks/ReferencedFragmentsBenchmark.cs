using System.Text;
using BenchmarkDotNet.Attributes;
using GraphQL.Execution;
using GraphQL.Validation;
using GraphQLParser.AST;

namespace GraphQL.Benchmarks;

[MemoryDiagnoser]
public class ReferencedFragmentsBenchmark : IBenchmark
{
    private GraphQLDocument _document;
    private GraphQLOperationDefinition _operation;
    private bool _onlyUsed;

    [Params("NoFragments", "UnusedFragment", "Typical", "Chain256", "Nested128", "Wide128", "Repeated256", "Directives")]
    public string Scenario { get; set; } = "Typical";

    [GlobalSetup]
    public void GlobalSetup()
    {
        var query = Scenario switch
        {
            "NoFragments" => "{ hero { id name friends { id name } } }",
            "UnusedFragment" => "{ hero { id name } } fragment Unused on Character { id }",
            "Typical" => """
                { hero { ...CharacterDetails friends { ...CharacterDetails ...FriendDetails } } }
                fragment CharacterDetails on Character { id name ...Appearance }
                fragment Appearance on Character { appearsIn }
                fragment FriendDetails on Character { friends { ...Appearance } }
                """,
            "Chain256" => BuildChain(256),
            "Nested128" => "{" + string.Concat(Enumerable.Repeat("child { ... on Character {", 128)) + "...F" + new string('}', 257) + " fragment F on Character { id }",
            "Wide128" => "{" + string.Join(" ", Enumerable.Range(0, 128).Select(i => $"...F{i}")) + "}" + string.Join(" ", Enumerable.Range(0, 128).Select(i => $"fragment F{i} on Character {{ id }}")),
            "Repeated256" => "{" + string.Concat(Enumerable.Repeat("hero { ...F ...G }", 256)) + "} fragment F on Character { id ...G } fragment G on Character { name }",
            "Directives" => """
                query($show: Boolean = true) {
                  hero { ...F @include(if: $show) ...G @skip(if: true) }
                  friends @include(if: false) { ...G }
                  ... on Query @skip(if: false) { hero { ...H } }
                }
                fragment F on Character { id ...H @include(if: true) }
                fragment G on Character { name }
                fragment H on Character { appearsIn }
                """,
            _ => throw new InvalidOperationException($"Unknown scenario: {Scenario}"),
        };
        _document = new GraphQLDocumentBuilder { MaxDepth = 1024 }.Build(query);
        _operation = (GraphQLOperationDefinition)_document.Definitions[0];
        _onlyUsed = Scenario == "Directives";
    }

    // Include construction of the per-request context; parse the reusable AST outside
    // the measurement. A fresh context ensures every invocation traverses the AST.
    [Benchmark]
    public List<GraphQLFragmentDefinition> FindFragments()
    {
        var context = new ValidationContext
        {
            Document = _document,
            Operation = _operation,
            Variables = Inputs.Empty,
        };
        return context.GetRecursivelyReferencedFragments(_operation, _onlyUsed);
    }

    private static string BuildChain(int count)
    {
        var query = new StringBuilder("{ ...F0 }");
        for (int i = 0; i < count; ++i)
        {
            query.Append(" fragment F").Append(i).Append(" on Character { ");
            if (i + 1 < count)
                query.Append("...F").Append(i + 1);
            else
                query.Append("id");
            query.Append(" }");
        }
        return query.ToString();
    }

    void IBenchmark.RunProfiler() => FindFragments();
}
