using System;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class InputObjectGraphTypeTests
    {
        [Fact]
        public void should_throw_an_exception_if_input_object_graph_type_contains_object_graph_type_field()
        {
            var type = new InputObjectGraphType();
            var exception = Should.Throw<ArgumentException>(() => type.Field<ObjectGraphType>().Name("test"));

            exception.Message.ShouldContain("InputObjectGraphType cannot have fields containing a ObjectGraphType.");
        }
    }
}
