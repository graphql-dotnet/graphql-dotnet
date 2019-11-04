using System;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class InputObjectGraphTypeTests
    {
        private class MyObjectGraphType : ObjectGraphType { }
        private class MyInputGraphType : InputObjectGraphType { }
        private class MyEnumGraphType : EnumerationGraphType { }

        [Fact]
        public void should_throw_an_exception_if_input_object_graph_type_contains_object_graph_type_field()
        {
            var type = new InputObjectGraphType();
            var exception = Should.Throw<ArgumentException>(() => type.Field<NonNullGraphType<ListGraphType<MyObjectGraphType>>>().Name("test"));

            exception.Message.ShouldContain("InputObjectGraphType should contain only fields of 'input types' - enumerations, scalars and other InputObjectGraphTypes");
        }

        [Fact]
        public void should_not_throw_an_exception_if_input_object_graph_type_doesnt_contains_object_graph_type_field()
        {
            var type = new InputObjectGraphType();
            var field1 = type.Field<NonNullGraphType<ListGraphType<MyInputGraphType>>>().Name("test1");
            var field2 = type.Field<StringGraphType>().Name("test2");
            var field3 = type.Field<MyEnumGraphType>().Name("test3");
        }
    }
}
