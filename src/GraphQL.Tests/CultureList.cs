using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace GraphQL.Tests
{
    public class CultureList : IEnumerable<object[]>
    {
        private readonly List<object[]> _data = new List<object[]>
        {
            new object[] { CultureInfo.InvariantCulture },
            new object[] { new CultureInfo("en-US") },
            new object[] { new CultureInfo("fi-FI") }
        };

        public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
