using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Tests.Introspection
{
    public class SchemaIntrospectionDocumentWritersTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var writer in DocumentWritersTestData.AllWriters)
            {
                yield return new object[] { writer, GetIntrospectionResult(writer) };
            }
        }

        private string GetIntrospectionResult(IDocumentWriter writer)
        {
            if (writer is NewtonsoftJson.DocumentWriter)
            {
                return IntrospectionResult.DataWhenNewtonsoftJson;
            }

            return IntrospectionResult.Data;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
