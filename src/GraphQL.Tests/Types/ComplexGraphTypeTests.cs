using GraphQL.StarWars.Types;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class ComplexGraphTypeTests
    {
        internal class ComplexType<T> : ComplexGraphType<T> {
            public ComplexType()
            {
                Name = typeof(T).Name;
            }
        }

        internal class TestObject
        {
            public int? someInt { get; set; }
            public KeyValuePair<int, string> valuePair { get; set; }
        }

        [Fact]
        public void accepts_property_expressions()
        {
            var type = new ComplexType<Droid>();
            var field = type.Field(d => d.Name);

            type.Fields.Last().Name.ShouldBe("Name");
            type.Fields.Last().Type.ShouldBe(typeof(NonNullGraphType<StringGraphType>));
        }

        [Fact]
        public void allows_custom_name()
        {
            var type = new ComplexType<Droid>();
            var field = type.Field(d => d.Name)
                .Name("droid");

            type.Fields.Last().Name.ShouldBe("droid");
        }

        [Fact]
        public void allows_nullable_types()
        {
            var type = new ComplexType<Droid>();

            type.Field("appearsIn", d => d.AppearsIn.First(), nullable: true);

            type.Fields.Last().Type.ShouldBe(typeof(IntGraphType));
        }

        [Fact]
        public void infers_from_nullable_types()
        {
            var type = new ComplexType<TestObject>();

            type.Field(d => d.someInt, nullable: true);

            type.Fields.Last().Type.ShouldBe(typeof(IntGraphType));
        }

        [Fact]
        public void throws_when_name_is_not_inferable()
        {
            var type = new ComplexType<Droid>();

            var exp = Should.Throw<ArgumentException>(() =>
                type.Field(d => d.AppearsIn.First())
            );
            exp.Message.ShouldBe(
                "Cannot infer a Field name from the expression: 'd.AppearsIn.First()' on parent GraphQL type: 'Droid'.");
        }

        [Fact]
        public void throws_when_type_is_not_inferable()
        {
            var type = new ComplexType<TestObject>();

            var exp = Should.Throw<ArgumentException>(() =>
                type.Field(d => d.valuePair)
            );
            exp.Message.ShouldStartWith(
                "The GraphQL type for Field: 'valuePair' on parent type: 'TestObject' could not be derived implicitly.");
        }

        [Fact]
        public void throws_when_type_is_incompatible()
        {
            var type = new ComplexType<TestObject>();

            var exp = Should.Throw<ArgumentException>(() =>
                type.Field(d => d.someInt)
            );

            exp.InnerException.Message.ShouldStartWith(
                "Explicitly nullable type: Nullable<Int32> cannot be coerced to a non nullable GraphQL type.");
        }

        [Fact]
        public void create_field_with_func_resolver()
        {
            var type = new ComplexType<Droid>();
            var field = type.Field<StringGraphType>("name",
                resolve: context => context.Source.Name
            );

            type.Fields.Last().Type.ShouldBe(typeof(StringGraphType));
        }
    }
}
