using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using GraphQLParser;

namespace GraphQL.Tests
{
    public class ROMToStringComparer : IEqualityComparer<object>
    {
        bool IEqualityComparer<object>.Equals(object x, object y)
            => EqualityComparer<object>.Default.Equals(Convert(x), Convert(y));

        private static object Convert(object x) => x switch
        {
            ROM a => (string)a,
            List<object> a => a.Select(i => Convert(i)).ToList(),
            _ => x
        };

        public int GetHashCode(
#if NET6_0_OR_GREATER
            [DisallowNull]
#endif
        object obj) => throw new NotImplementedException();
    }
}
