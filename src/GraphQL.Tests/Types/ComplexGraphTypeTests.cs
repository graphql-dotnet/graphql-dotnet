using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using GraphQL.Conversion;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using GraphQL.Utilities;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
#pragma warning disable 618

    public class ComplexGraphTypeTests
    {
        internal class ComplexType<T> : ComplexGraphType<T>
        {
            public ComplexType()
            {
                Name = typeof(T).GetFriendlyName().Replace("<", "Of").Replace(">", "");
            }
        }

        internal class GenericFieldType<T> : FieldType { }

        [Description("Object for test")]
        [Obsolete("Obsolete for test")]
        internal class TestObjectIn
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
            /// <summary>
            /// Description from xml comment
            /// </summary>
            public short someShort { get; set; }
            public ushort someUShort { get; set; }
            public ulong someULong { get; set; }
            public uint someUInt { get; set; }
            public IEnumerable someEnumerable { get; set; }
            public IEnumerable<string> someEnumerableOfString { get; set; }
            [Required]
            public string someRequiredString { get; set; }
            public Direction someEnum { get; set; }
            public Direction? someNullableEnum { get; set; }
            public List<int?> someListWithNullable { get; set; }
            [Required]
            public List<int> someRequiredList { get; set; }
            [Required]
            public List<int?> someRequiredListWithNullable { get; set; }
            public int someNotNullInt { get; set; }
            public MoneyIn someMoney { get; set; }
        }

        [Description("Object for test")]
        [Obsolete("Obsolete for test")]
        internal class TestObjectOut
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
            /// <summary>
            /// Description from xml comment
            /// </summary>
            public short someShort { get; set; }
            public ushort someUShort { get; set; }
            public ulong someULong { get; set; }
            public uint someUInt { get; set; }
            public IEnumerable someEnumerable { get; set; }
            public IEnumerable<string> someEnumerableOfString { get; set; }
            [Required]
            public string someRequiredString { get; set; }
            public Direction someEnum { get; set; }
            public Direction? someNullableEnum { get; set; }
            public List<int?> someListWithNullable { get; set; }
            [Required]
            public List<int> someRequiredList { get; set; }
            [Required]
            public List<int?> someRequiredListWithNullable { get; set; }
            public int someNotNullInt { get; set; }
            public MoneyOut someMoney { get; set; }
        }

        internal class MoneyIn
        {
            public decimal Amount { get; set; }
            public string Currency { get; set; }
        }

        internal class MoneyOut
        {
            public decimal Amount { get; set; }
            public string Currency { get; set; }
        }

        internal enum Direction
        {
            Asc,
            /// <summary>
            /// Descending Order
            /// </summary>
            Desc,
            [Obsolete("Do not use Random. This makes no sense!")]
            Random
        }

        //TODO: remove in/out copy-paste after https://github.com/graphql-dotnet/graphql-dotnet/issues/2189 is solved
        [Fact]
        public void auto_register_object_graph_type()
        {
            GraphTypeTypeRegistry.Register<MoneyOut, AutoRegisteringObjectGraphType<MoneyOut>>();

            var type = new AutoRegisteringObjectGraphType<TestObjectOut>(o => o.valuePair, o => o.someEnumerable);
            type.Name.ShouldBe(nameof(TestObjectOut));
            type.Description.ShouldBe("Object for test");
            type.DeprecationReason.ShouldBe("Obsolete for test");
            type.Fields.Count().ShouldBe(18);
            type.Fields.First(f => f.Name == nameof(TestObjectOut.someString)).Description.ShouldBe("Super secret");
            type.Fields.First(f => f.Name == nameof(TestObjectOut.someString)).Type.ShouldBe(typeof(StringGraphType));
            type.Fields.First(f => f.Name == nameof(TestObjectOut.someRequiredString)).Type.ShouldBe(typeof(NonNullGraphType<StringGraphType>));
            type.Fields.First(f => f.Name == nameof(TestObjectOut.someInt)).Type.ShouldBe(typeof(IntGraphType));
            type.Fields.First(f => f.Name == nameof(TestObjectOut.someNotNullInt)).Type.ShouldBe(typeof(NonNullGraphType<IntGraphType>));
            type.Fields.First(f => f.Name == nameof(TestObjectOut.someBoolean)).DeprecationReason.ShouldBe("Use someInt");
            type.Fields.First(f => f.Name == nameof(TestObjectOut.someDate)).DefaultValue.ShouldBe(new DateTime(2019, 3, 14));
            type.Fields.First(f => f.Name == nameof(TestObjectOut.someShort)).Description.ShouldBe("Description from xml comment");
            type.Fields.First(f => f.Name == nameof(TestObjectOut.someEnumerableOfString)).Type.ShouldBe(typeof(ListGraphType<StringGraphType>));
            type.Fields.First(f => f.Name == nameof(TestObjectOut.someEnum)).Type.ShouldBe(typeof(NonNullGraphType<EnumerationGraphType<Direction>>));
            type.Fields.First(f => f.Name == nameof(TestObjectOut.someNullableEnum)).Type.ShouldBe(typeof(EnumerationGraphType<Direction>));
            type.Fields.First(f => f.Name == nameof(TestObjectOut.someList)).Type.ShouldBe(typeof(ListGraphType<NonNullGraphType<IntGraphType>>));
            type.Fields.First(f => f.Name == nameof(TestObjectOut.someListWithNullable)).Type.ShouldBe(typeof(ListGraphType<IntGraphType>));
            type.Fields.First(f => f.Name == nameof(TestObjectOut.someRequiredList)).Type.ShouldBe(typeof(NonNullGraphType<ListGraphType<NonNullGraphType<IntGraphType>>>));
            type.Fields.First(f => f.Name == nameof(TestObjectOut.someRequiredListWithNullable)).Type.ShouldBe(typeof(NonNullGraphType<ListGraphType<IntGraphType>>));
            type.Fields.First(f => f.Name == nameof(TestObjectOut.someMoney)).Type.ShouldBe(typeof(AutoRegisteringObjectGraphType<MoneyOut>));

            var enumType = new EnumerationGraphType<Direction>();
            enumType.Values["DESC"].Description.ShouldBe("Descending Order");
            enumType.Values["RANDOM"].DeprecationReason.ShouldBe("Do not use Random. This makes no sense!");
        }

        [Fact]
        public void auto_register_input_object_graph_type()
        {
            GraphTypeTypeRegistry.Register<MoneyIn, AutoRegisteringInputObjectGraphType<MoneyIn>>();

            var type = new AutoRegisteringInputObjectGraphType<TestObjectIn>(o => o.valuePair, o => o.someEnumerable);
            type.Name.ShouldBe(nameof(TestObjectIn));
            type.Description.ShouldBe("Object for test");
            type.DeprecationReason.ShouldBe("Obsolete for test");
            type.Fields.Count().ShouldBe(18);
            type.Fields.First(f => f.Name == nameof(TestObjectIn.someString)).Description.ShouldBe("Super secret");
            type.Fields.First(f => f.Name == nameof(TestObjectIn.someString)).Type.ShouldBe(typeof(StringGraphType));
            type.Fields.First(f => f.Name == nameof(TestObjectIn.someRequiredString)).Type.ShouldBe(typeof(NonNullGraphType<StringGraphType>));
            type.Fields.First(f => f.Name == nameof(TestObjectIn.someInt)).Type.ShouldBe(typeof(IntGraphType));
            type.Fields.First(f => f.Name == nameof(TestObjectIn.someNotNullInt)).Type.ShouldBe(typeof(NonNullGraphType<IntGraphType>));
            type.Fields.First(f => f.Name == nameof(TestObjectIn.someBoolean)).DeprecationReason.ShouldBe("Use someInt");
            type.Fields.First(f => f.Name == nameof(TestObjectIn.someDate)).DefaultValue.ShouldBe(new DateTime(2019, 3, 14));
            type.Fields.First(f => f.Name == nameof(TestObjectIn.someShort)).Description.ShouldBe("Description from xml comment");
            type.Fields.First(f => f.Name == nameof(TestObjectIn.someEnumerableOfString)).Type.ShouldBe(typeof(ListGraphType<StringGraphType>));
            type.Fields.First(f => f.Name == nameof(TestObjectIn.someEnum)).Type.ShouldBe(typeof(NonNullGraphType<EnumerationGraphType<Direction>>));
            type.Fields.First(f => f.Name == nameof(TestObjectIn.someNullableEnum)).Type.ShouldBe(typeof(EnumerationGraphType<Direction>));
            type.Fields.First(f => f.Name == nameof(TestObjectIn.someList)).Type.ShouldBe(typeof(ListGraphType<NonNullGraphType<IntGraphType>>));
            type.Fields.First(f => f.Name == nameof(TestObjectIn.someListWithNullable)).Type.ShouldBe(typeof(ListGraphType<IntGraphType>));
            type.Fields.First(f => f.Name == nameof(TestObjectIn.someRequiredList)).Type.ShouldBe(typeof(NonNullGraphType<ListGraphType<NonNullGraphType<IntGraphType>>>));
            type.Fields.First(f => f.Name == nameof(TestObjectIn.someRequiredListWithNullable)).Type.ShouldBe(typeof(NonNullGraphType<ListGraphType<IntGraphType>>));
            type.Fields.First(f => f.Name == nameof(TestObjectIn.someMoney)).Type.ShouldBe(typeof(AutoRegisteringInputObjectGraphType<MoneyIn>));

            var enumType = new EnumerationGraphType<Direction>();
            enumType.Values["DESC"].Description.ShouldBe("Descending Order");
            enumType.Values["RANDOM"].DeprecationReason.ShouldBe("Do not use Random. This makes no sense!");
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
            var type = new ComplexType<TestObjectOut>();

            type.Field(d => d.someInt, nullable: true);

            type.Fields.Last().Type.ShouldBe(typeof(IntGraphType));
        }

        [Fact]
        public void infers_from_list_types()
        {
            var type = new ComplexType<TestObjectOut>();

            type.Field(d => d.someList, nullable: true);

            type.Fields.Last().Type.ShouldBe(typeof(ListGraphType<NonNullGraphType<IntGraphType>>));
        }

        [Fact]
        public void infers_field_description_from_expression()
        {
            var type = new ComplexType<TestObjectOut>();
            var field = type.Field(d => d.someString);

            type.Fields.Last().Description.ShouldBe("Super secret");
        }

        [Fact]
        public void infers_field_deprecation_from_expression()
        {
            var type = new ComplexType<TestObjectOut>();
            var field = type.Field(d => d.someBoolean);

            type.Fields.Last().DeprecationReason.ShouldBe("Use someInt");
        }

        [Fact]
        public void infers_field_default_from_expression()
        {
            var type = new ComplexType<TestObjectOut>();
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
            var type = new ComplexType<TestObjectOut>();

            var exp = Should.Throw<ArgumentException>(() =>
                type.Field(d => d.valuePair)
            );
            exp.Message.ShouldStartWith(
                "The GraphQL type for Field: 'valuePair' on parent type: 'TestObjectOut' could not be derived implicitly.");
        }

        [Fact]
        public void throws_when_type_is_incompatible()
        {
            var type = new ComplexType<TestObjectOut>();

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

            exception.ParamName.ShouldBe("fieldType");
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

            exception.ParamName.ShouldBe("fieldType");
            exception.Message.ShouldStartWith("The declared field 'genericname' on 'ListOfDroid' requires a field 'Type' when no 'ResolvedType' is provided.");
        }

        private Exception test_field_name(string fieldName)
        {
            // test failure
            return Should.Throw<ArgumentOutOfRangeException>(() =>
            {
                var type = new ObjectGraphType();
                type.Field<StringGraphType>(fieldName);
                var schema = new Schema
                {
                    Query = type
                };
                schema.Initialize();
            });
        }

        [Theory]
        [InlineData("__id")]
        [InlineData("___id")]
        public void throws_when_field_name_prefix_with_reserved_underscores(string fieldName)
        {
            var exception = test_field_name(fieldName);

            exception.Message.ShouldStartWith($"A field name: '{fieldName}' must not begin with \"__\", which is reserved by GraphQL introspection.");
        }

        [Theory]
        [InlineData("i#d")]
        [InlineData("i$d")]
        [InlineData("id$")]
        public void throws_when_field_name_doesnot_follow_spec(string fieldName)
        {
            var exception = test_field_name(fieldName);

            exception.Message.ShouldStartWith($"A field name must match /^[_a-zA-Z][_a-zA-Z0-9]*$/ but '{fieldName}' does not.");
        }

        [Theory]
        [InlineData("__id")]
        [InlineData("___id")]
        [InlineData("i#d")]
        [InlineData("i$d")]
        [InlineData("id$")]
        public void does_not_throw_with_filtering_nameconverter(string fieldName)
        {
            NameValidator.Validation = (n, t) => { }; // disable "before" checks

            try
            {
                var type = new ObjectGraphType();
                type.Field<StringGraphType>(fieldName);
                var schema = new Schema
                {
                    Query = type,
                    NameConverter = new TestNameConverter(fieldName, "pass")
                };
                schema.Initialize();
            }
            finally
            {
                NameValidator.Validation = NameValidator.ValidateDefault; // restore defaults
            }
        }

        [Fact]
        public void throws_with_bad_namevalidator()
        {
            var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            {
                var type = new ObjectGraphType();
                type.Field<StringGraphType>("hello");
                var schema = new Schema
                {
                    Query = type,
                    NameConverter = new TestNameConverter("hello", "hello$")
                };
                schema.Initialize();
            });

            exception.Message.ShouldStartWith($"A field name must match /^[_a-zA-Z][_a-zA-Z0-9]*$/ but 'hello$' does not.");
        }

        private class TestNameConverter : INameConverter
        {
            private readonly string _from;
            private readonly string _to;
            public TestNameConverter(string from, string to)
            {
                _from = from;
                _to = to;
            }

            public string NameForArgument(string argumentName, IComplexGraphType parentGraphType, FieldType field)
                => argumentName == _from ? _to : argumentName;

            public string NameForField(string fieldName, IComplexGraphType parentGraphType)
                => fieldName == _from ? _to : fieldName;
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void throws_when_field_name_is_null_or_empty(string fieldName)
        {
            var type = new ComplexType<TestObjectOut>();
            var exception = Should.Throw<ArgumentOutOfRangeException>(() => type.Field<StringGraphType>(fieldName));

            exception.Message.ShouldStartWith("A field name can not be null or empty.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void throws_when_field_name_is_null_or_empty_using_field_builder(string fieldName)
        {
            var type = new ComplexType<TestObjectOut>();
            var exception = Should.Throw<ArgumentOutOfRangeException>(() => type.Field<StringGraphType>().Name(fieldName));

            exception.Message.ShouldStartWith("A field name can not be null or empty.");
        }

        [Theory]
        [InlineData("name")]
        [InlineData("Name")]
        [InlineData("_name")]
        [InlineData("test_name")]
        public void should_not_throw_exception_on_valid_field_name(string fieldName)
        {
            var type = new ComplexType<TestObjectOut>();
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
            var type = new ComplexType<TestObjectOut>();
            type.Field<StringGraphType>().Name(fieldName);

            type.Fields.Last().Name.ShouldBe(fieldName);
        }
    }

#pragma warning restore 618
}
