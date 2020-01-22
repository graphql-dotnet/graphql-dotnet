using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Tests
{
    public class DocumentWritersTestData : IEnumerable<object[]>
    {

        private readonly List<object[]> _data = new List<object[]>
        {
            new object[] { new SystemTextJson.DocumentWriter(indent: true) },
            new object[] { new NewtonsoftJson.DocumentWriter(indent: true) }
        };

        public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
