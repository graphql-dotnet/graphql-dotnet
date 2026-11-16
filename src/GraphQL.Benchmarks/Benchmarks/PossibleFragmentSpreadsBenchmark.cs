using System.Text;
using BenchmarkDotNet.Attributes;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Rules;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Benchmarks;

[MemoryDiagnoser]
public class PossibleFragmentSpreadsBenchmark
{
    private ISchema _schema;
    private GraphQLDocument _document;
    private GraphQLOperationDefinition _operation;

    [Params(0, 10, 1_000, 10_000)]
    public int FragmentCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _schema = Schema.For("type Query { value: String }");
        _schema.Initialize();

        var query = new StringBuilder("query Test { ");
        query.Append(FragmentCount == 0 ? "value" : "...F0").AppendLine(" }");
        for (int i = 0; i < FragmentCount; ++i)
        {
            query.Append("fragment F").Append(i).Append(" on Query { value");
            if (i + 1 < FragmentCount)
                query.Append(" ...F").Append(i + 1);
            query.AppendLine(" }");
        }
        _document = Parser.Parse(query.ToString());
        _operation = (GraphQLOperationDefinition)_document.Definitions[0];
        RuleVisitors().GetAwaiter().GetResult();
    }

    // Only run this rule and TypeInfo: argument coercion and execution have
    // their own fragment traversal and would obscure the rule's scaling.
    [Benchmark]
    public async Task RuleVisitors()
    {
        var context = new ValidationContext
        {
            Schema = _schema,
            Document = _document,
            Operation = _operation,
            TypeInfo = new TypeInfo(_schema),
        };
        var visitor = await PossibleFragmentSpreads.Instance.GetPreNodeVisitorAsync(context).ConfigureAwait(false);
        var walker = visitor == null
            ? new BasicVisitor(context.TypeInfo)
            : new BasicVisitor(context.TypeInfo, visitor);
        await walker.VisitAsync(_document, new BasicVisitor.State(context)).ConfigureAwait(false);
        if (context.HasErrors)
            throw new InvalidOperationException("The benchmark document must be valid.");
    }
}

