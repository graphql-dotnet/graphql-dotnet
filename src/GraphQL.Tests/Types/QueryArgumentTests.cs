using System;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class QueryArgumentTests
    {
        [Fact]
        public void throws_exception_with_null_instance_type()
        {
            IGraphType type = null;
            Should.Throw<ArgumentOutOfRangeException>(() => new QueryArgument(type));
        }

        [Fact]
        public void throws_exception_with_null_type()
        {
            Type type = null;
            Should.Throw<ArgumentOutOfRangeException>(() => new QueryArgument(type));
        }

        [Fact]
        public void throws_exception_with_invalid_type()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => new QueryArgument(typeof(string)));
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
