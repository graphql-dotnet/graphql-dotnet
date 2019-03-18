using GraphQL.StarWars.Types;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Xunit;
using System.ComponentModel;

namespace GraphQL.Tests.Types
{
    public class ComplexGraphTypeTests
    {
        internal class ComplexType<T> : ComplexGraphType<T> {
            public ComplexType()
            {
                Name = typeof(T).GetFriendlyName();
            }
        }

        internal class GenericFieldType<T> : FieldType { }

        internal class TestObject
        {
            public int? someInt { get; set; }
            public KeyValuePair<int, string> valuePair { get; set; }
            public List<int> someList { get; set; }
            [Description("Super secret")]
            public string someString { get; set; }
            [Obsolete("Use someInt")]
            public bool someBoolean { get; set; }
            [DefaultValue(typeof(DateTime), "2019/03/14")]
            public DateTime someDate { get; set; }
        }

        [Fact]
        public void auto_register()
        {
            var type = new AutoRegisteringObjectGraphType<TestObject>(o => o.valuePair);

            type.Fields.Count().ShouldBe(4);
            type.Fields.First(f => f.Name == "someString").Description.ShouldBe("Super secret");
            type.Fields.First(f => f.Name == "someBoolean").DeprecationReason.ShouldBe("Use someInt");
            type.Fields.First(f => f.Name == "someDate").DefaultValue.ShouldBe(new DateTime(2019, 3, 14));
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
        public void infers_from_list_types()
        {
            var type = new ComplexType<TestObject>();

            type.Field(d => d.someList, nullable: true);

            type.Fields.Last().Type.ShouldBe(typeof(ListGraphType<IntGraphType>));
        }

        [Fact]
        public void infers_field_description_from_expression()
        {
            var type = new ComplexType<TestObject>();
            var field = type.Field(d => d.someString);

            type.Fields.Last().Description.ShouldBe("Super secret");
        }

        [Fact]
        public void infers_field_deprecation_from_expression()
        {
            var type = new ComplexType<TestObject>();
            var field = type.Field(d => d.someBoolean);

            type.Fields.Last().DeprecationReason.ShouldBe("Use someInt");
        }

        [Fact]
        public void infers_field_default_from_expression()
        {
            var type = new ComplexType<TestObject>();
            var field = type.Field(d => d.someDate);

            type.Fields.Last().DefaultValue.ShouldBe(new DateTime(2019, 3, 14));
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

        [Fact]
        public void throws_informative_exception_when_no_types_defined()
        {
            var type = new ComplexType<Droid>();

            var fieldType = new FieldType
            {
                Name = "name",
                ResolvedType = null,
                Type = null,
            };

            var exception = Should.Throw<ArgumentOutOfRangeException>(() => type.AddField(fieldType));

            exception.ParamName.ShouldBe("Type");
            exception.Message.ShouldStartWith("The declared field 'name' on 'Droid' requires a field 'Type' when no 'ResolvedType' is provided.");
        }

        [Fact]
        public void throws_informative_exception_when_no_types_defined_on_more_generic_type()
        {
            var type = new ComplexType<List<Droid>>();

            var fieldType = new GenericFieldType<List<Droid>>
            {
                Name = "genericname",
                ResolvedType = null,
                Type = null,
            };

            var exception = Should.Throw<ArgumentOutOfRangeException>(() => type.AddField(fieldType));

            exception.ParamName.ShouldBe("Type");
            exception.Message.ShouldStartWith("The declared field 'genericname' on 'List<Droid>' requires a field 'Type' when no 'ResolvedType' is provided.");
        }

        [Theory]
        [InlineData("__id")]
        [InlineData("___id")]
        public void throws_when_field_name_prefix_with_reserved_underscores(string fieldName)
        {
            var type = new ComplexType<TestObject>();
            var exception = Should.Throw<ArgumentOutOfRangeException>(() => type.Field<StringGraphType>(fieldName));

            exception.Message.ShouldStartWith($"A field name: {fieldName} must not begin with \"__\", which is reserved by GraphQL introspection.");
        }

        [Theory]
        [InlineData("i#d")]
        [InlineData("i$d")]
        [InlineData("id$")]
        public void throws_when_field_name_doesnot_follow_spec(string fieldName)
        {
            var type = new ComplexType<TestObject>();
            var exception = Should.Throw<ArgumentOutOfRangeException>(() => type.Field<StringGraphType>(fieldName));

            exception.Message.ShouldStartWith($"A field name must match /^[_a-zA-Z][_a-zA-Z0-9]*$/ but {fieldName} does not.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void throws_when_field_name_is_null_or_empty(string fieldName)
        {
            var type = new ComplexType<TestObject>();
            var exception = Should.Throw<ArgumentOutOfRangeException>(() => type.Field<StringGraphType>(fieldName));

            exception.Message.ShouldStartWith($"A field name can not be null or empty.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void throws_when_field_name_is_null_or_empty_using_field_builder(string fieldName)
        {
            var type = new ComplexType<TestObject>();
            var exception = Should.Throw<ArgumentOutOfRangeException>(() => type.Field<StringGraphType>().Name(fieldName));

            exception.Message.ShouldStartWith($"A field name can not be null or empty.");
        }

        [Theory]
        [InlineData("name")]
        [InlineData("Name")]
        [InlineData("_name")]
        [InlineData("test_name")]
        public void should_not_throw_exception_on_valid_field_name(string fieldName)
        {
            var type = new ComplexType<TestObject>();
            var field = type.Field<StringGraphType>(fieldName);

            field.Name.ShouldBe(fieldName);
        }

        [Theory]
        [InlineData("name")]
        [InlineData("Name")]
        [InlineData("_name")]
        [InlineData("test_name")]
        public void should_not_throw_exception_on_valid_field_name_using_field_builder(string fieldName)
        {
            var type = new ComplexType<TestObject>();
            type.Field<StringGraphType>().Name(fieldName);

            type.Fields.Last().Name.ShouldBe(fieldName);
        }
    }
}
