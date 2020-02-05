using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Tests
{
    public class DocumentWritersTestData : IEnumerable<object[]>
    {
        public static readonly List<IDocumentWriter> AllWriters = new List<IDocumentWriter>
        {
            new SystemTextJson.DocumentWriter(indent: true),
            new NewtonsoftJson.DocumentWriter(indent: true)
        };

        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var writer in AllWriters)
            {
                yield return new object[] { new SystemTextJson.DocumentWriter(indent: true) };
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
