using System;
using GraphQL.Types;

namespace GraphQL.Tests.Types
{
    public class QueryArgumentTests
    {
        [Test]
        public void throws_exception_with_null_type()
        {
            Expect.Throws<ArgumentOutOfRangeException>(() => new QueryArgument(null));
        }

        [Test]
        public void throws_exception_with_invalid_type()
        {
            Expect.Throws<ArgumentOutOfRangeException>(() => new QueryArgument(typeof(string)));
        }

        [Test]
        public void does_not_throw_with_valid_type()
        {
            new QueryArgument(typeof(GraphType));
        }

        [Test]
        public void does_not_throw_with_object_type()
        {
            new QueryArgument(typeof(ObjectGraphType));
        }
    }
}
