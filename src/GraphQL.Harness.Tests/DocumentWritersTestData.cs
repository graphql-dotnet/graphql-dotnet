using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Harness.Tests
{
    public class DocumentWritersTestData : IEnumerable<object[]>
    {

        private readonly List<object[]> _data = new List<object[]>
        {
            new object[] { new SystemTextJson.DocumentWriter() },
            new object[] { new NewtonsoftJson.DocumentWriter() }
        };

        public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
