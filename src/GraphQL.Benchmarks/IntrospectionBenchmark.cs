using BenchmarkDotNet.Attributes;
using GraphQL.Conversion;
using GraphQL.Introspection;
using GraphQL.Types;

namespace GraphQL.Benchmarks
{
    [MemoryDiagnoser]
    public class IntrospectionBenchmark
    {
        private ISchema _schema;
        private DocumentExecuter _executer;
        private ExecutionOptions _options;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _executer = new DocumentExecuter();
            _schema = new SchemaForIntrospection();
            _options = new ExecutionOptions
            {
                Schema = _schema,
                Query = SchemaIntrospection.IntrospectionQuery,
                FieldNameConverter = CamelCaseFieldNameConverter.Instance
            };
        }

        [Benchmark]
        public void Introspection()
        {
            _executer.ExecuteAsync(_options).GetAwaiter().GetResult();
        }
    }
}
