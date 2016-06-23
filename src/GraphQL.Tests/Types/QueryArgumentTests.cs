using System;
using GraphQL.Types;

namespace GraphQL.Tests.Types
{
    public class QueryArgumentTests
    {
        [Fact]
        public void throws_exception_with_null_type()
        {
            Expect.Throws<ArgumentOutOfRangeException>(() => new QueryArgument(null));
        }

        [Fact]
        public void throws_exception_with_invalid_type()
        {
            Expect.Throws<ArgumentOutOfRangeException>(() => new QueryArgument(typeof(string)));
        }

        [Fact]
        public void does_not_throw_with_valid_type()
        {
            new QueryArgument(typeof(GraphType));
        }

        [Fact]
        public void does_not_throw_with_object_type()
        {
            new QueryArgument(typeof(ObjectGraphType));
        }
    }
}
