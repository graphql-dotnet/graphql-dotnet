using System;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class FieldRegistrationTests
    {
        [Fact]
        public void throws_error_when_trying_to_register_field_with_same_name()
        {
            var graphType = new ObjectGraphType();
            graphType.Field<StringGraphType>("id");

            Should.Throw<ArgumentOutOfRangeException>(
                () => graphType.Field<StringGraphType>("id")
            );
        }

        [Fact]
        public void can_register_field_of_compatible_type()
        {
            var graphType = new ObjectGraphType();
            graphType.Field(typeof(BooleanGraphType), "isValid").Type.ShouldBe(typeof(BooleanGraphType));
        }

        [Fact]
        public void throws_error_when_trying_to_register_field_of_incompatible_type()
        {
            var graphType = new ObjectGraphType();

            Should.Throw<ArgumentOutOfRangeException>(
                () => graphType.Field(typeof(string), "id")
            );
        }
    }
}
