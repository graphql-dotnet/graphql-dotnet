using GraphQL.Http;
using GraphQL.Introspection;
using GraphQL.Types;
using Should;

namespace GraphQL.Tests.Introspection
{
    public class SchemaIntrospectionTests
    {
        [Fact]
        public void validate_core_schema()
        {
            var documentExecuter = new DocumentExecuter();
            var executionResult = documentExecuter.ExecuteAsync(new Schema(), null, SchemaIntrospection.IntrospectionQuery, null).Result;
            var json = new DocumentWriter(true).Write(executionResult.Data);

            json.ShouldEqual(IntrospectionResult.Data);
        }
    }
}
