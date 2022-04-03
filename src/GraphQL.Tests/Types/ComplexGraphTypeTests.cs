using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using GraphQL.Conversion;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Tests.Types;

[Collection("StaticTests")]
public class ComplexGraphTypeTests
{
    internal class ComplexType<T> : ObjectGraphType<T>
    {
        public ComplexType()
        {
            Name = typeof(T).GetFriendlyName().Replace("<", "Of").Replace(">", "");
        }
    }

    internal class GenericFieldType<T> : FieldType { }

    [Description("Object for test")]
    [Obsolete("Obsolete for test")]
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
        /// <summary>
        /// Description from XML comment
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
        public Money someMoney { get; set; }
    }

    [GraphQLMetadata(InputType = typeof(AutoRegisteringInputObjectGraphType<Money>), OutputType = typeof(AutoRegisteringObjectGraphType<Money>))]
    internal class Money
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

    [Fact]
    public void auto_register_object_graph_type()
    {
        try
        {
            GlobalSwitches.EnableReadDescriptionFromXmlDocumentation = true;
            var schema = new Schema();
            var type = new AutoRegisteringObjectGraphType<TestObject>(o => o.valuePair, o => o.someEnumerable);
            schema.Query = type;
            schema.Initialize();

            type.Name.ShouldBe(nameof(TestObject));
            type.Description.ShouldBe("Object for test");
            type.DeprecationReason.ShouldBe("Obsolete for test");
            type.Fields.Count.ShouldBe(18);
            type.Fields.First(f => f.Name == nameof(TestObject.someString)).Description.ShouldBe("Super secret");
            type.Fields.First(f => f.Name == nameof(TestObject.someString)).Type.ShouldBe(typeof(StringGraphType));
            type.Fields.First(f => f.Name == nameof(TestObject.someRequiredString)).Type.ShouldBe(typeof(NonNullGraphType<StringGraphType>));
            type.Fields.First(f => f.Name == nameof(TestObject.someInt)).Type.ShouldBe(typeof(IntGraphType));
            type.Fields.First(f => f.Name == nameof(TestObject.someNotNullInt)).Type.ShouldBe(typeof(NonNullGraphType<IntGraphType>));
            type.Fields.First(f => f.Name == nameof(TestObject.someBoolean)).DeprecationReason.ShouldBe("Use someInt");
            type.Fields.First(f => f.Name == nameof(TestObject.someShort)).Description.ShouldBe("Description from XML comment");
            type.Fields.First(f => f.Name == nameof(TestObject.someEnumerableOfString)).Type.ShouldBe(typeof(ListGraphType<StringGraphType>));
            type.Fields.First(f => f.Name == nameof(TestObject.someEnum)).Type.ShouldBe(typeof(NonNullGraphType<EnumerationGraphType<Direction>>));
            type.Fields.First(f => f.Name == nameof(TestObject.someNullableEnum)).Type.ShouldBe(typeof(EnumerationGraphType<Direction>));
            type.Fields.First(f => f.Name == nameof(TestObject.someList)).Type.ShouldBe(typeof(ListGraphType<NonNullGraphType<IntGraphType>>));
            type.Fields.First(f => f.Name == nameof(TestObject.someListWithNullable)).Type.ShouldBe(typeof(ListGraphType<IntGraphType>));
            type.Fields.First(f => f.Name == nameof(TestObject.someRequiredList)).Type.ShouldBe(typeof(NonNullGraphType<ListGraphType<NonNullGraphType<IntGraphType>>>));
            type.Fields.First(f => f.Name == nameof(TestObject.someRequiredListWithNullable)).Type.ShouldBe(typeof(NonNullGraphType<ListGraphType<IntGraphType>>));
            type.Fields.First(f => f.Name == nameof(TestObject.someMoney)).Type.ShouldBe(typeof(AutoRegisteringObjectGraphType<Money>));

            var enumType = new EnumerationGraphType<Direction>();
            enumType.Values["DESC"].Description.ShouldBe("Descending Order");
            enumType.Values["RANDOM"].DeprecationReason.ShouldBe("Do not use Random. This makes no sense!");
        }
        finally
        {
            GlobalSwitches.EnableReadDescriptionFromXmlDocumentation = false;
        }
    }

    [Fact]
    public void auto_register_input_object_graph_type()
    {
        try
        {
            GlobalSwitches.EnableReadDescriptionFromXmlDocumentation = true;
            var schema = new Schema();
            var type = new AutoRegisteringInputObjectGraphType<TestObject>(o => o.valuePair, o => o.someEnumerable);
            var query = new ObjectGraphType();
            query.Field<StringGraphType>("test", arguments: new QueryArguments(new QueryArgument(type) { Name = "input" }));
            schema.Query = query;
            schema.Initialize();

            type.Name.ShouldBe(nameof(TestObject));
            type.Description.ShouldBe("Object for test");
            type.DeprecationReason.ShouldBe("Obsolete for test");
            type.Fields.Count.ShouldBe(18);
            type.Fields.First(f => f.Name == nameof(TestObject.someString)).Description.ShouldBe("Super secret");
            type.Fields.First(f => f.Name == nameof(TestObject.someString)).Type.ShouldBe(typeof(StringGraphType));
            type.Fields.First(f => f.Name == nameof(TestObject.someRequiredString)).Type.ShouldBe(typeof(NonNullGraphType<StringGraphType>));
            type.Fields.First(f => f.Name == nameof(TestObject.someInt)).Type.ShouldBe(typeof(IntGraphType));
            type.Fields.First(f => f.Name == nameof(TestObject.someNotNullInt)).Type.ShouldBe(typeof(NonNullGraphType<IntGraphType>));
            type.Fields.First(f => f.Name == nameof(TestObject.someBoolean)).DeprecationReason.ShouldBe("Use someInt");
            type.Fields.First(f => f.Name == nameof(TestObject.someDate)).DefaultValue.ShouldBe(new DateTime(2019, 3, 14));
            type.Fields.First(f => f.Name == nameof(TestObject.someShort)).Description.ShouldBe("Description from XML comment");
            type.Fields.First(f => f.Name == nameof(TestObject.someEnumerableOfString)).Type.ShouldBe(typeof(ListGraphType<StringGraphType>));
            type.Fields.First(f => f.Name == nameof(TestObject.someEnum)).Type.ShouldBe(typeof(NonNullGraphType<EnumerationGraphType<Direction>>));
            type.Fields.First(f => f.Name == nameof(TestObject.someNullableEnum)).Type.ShouldBe(typeof(EnumerationGraphType<Direction>));
            type.Fields.First(f => f.Name == nameof(TestObject.someList)).Type.ShouldBe(typeof(ListGraphType<NonNullGraphType<IntGraphType>>));
            type.Fields.First(f => f.Name == nameof(TestObject.someListWithNullable)).Type.ShouldBe(typeof(ListGraphType<IntGraphType>));
            type.Fields.First(f => f.Name == nameof(TestObject.someRequiredList)).Type.ShouldBe(typeof(NonNullGraphType<ListGraphType<NonNullGraphType<IntGraphType>>>));
            type.Fields.First(f => f.Name == nameof(TestObject.someRequiredListWithNullable)).Type.ShouldBe(typeof(NonNullGraphType<ListGraphType<IntGraphType>>));
            type.Fields.First(f => f.Name == nameof(TestObject.someMoney)).Type.ShouldBe(typeof(AutoRegisteringInputObjectGraphType<Money>));

            var enumType = new EnumerationGraphType<Direction>();
            enumType.Values["DESC"].Description.ShouldBe("Descending Order");
            enumType.Values["RANDOM"].DeprecationReason.ShouldBe("Do not use Random. This makes no sense!");
        }
        finally
        {
            GlobalSwitches.EnableReadDescriptionFromXmlDocumentation = false;
        }
    }

    [Fact]
    public void accepts_property_expressions()
    {
        var schema = new Schema();
        var type = new ComplexType<Droid>();
        _ = type.Field(d => d.Name);
        schema.Query = type;
        schema.Initialize();

        type.Fields.Last().Name.ShouldBe("name");
        type.Fields.Last().Type.ShouldBe(typeof(NonNullGraphType<StringGraphType>));
    }

    [Fact]
    public void allows_custom_name()
    {
        var type = new ComplexType<Droid>();
        _ = type.Field(d => d.Name)
            .Name("droid");

        type.Fields.Last().Name.ShouldBe("droid");
    }

    [Fact]
    public void allows_nullable_types()
    {
        var schema = new Schema();
        var type = new ComplexType<Droid>();
        type.Field("appearsIn", d => d.AppearsIn.First(), nullable: true);
        schema.Query = type;
        schema.Initialize();

        type.Fields.Last().Type.ShouldBe(typeof(IntGraphType));
    }

    [Fact]
    public void infers_from_nullable_types()
    {
        var schema = new Schema();
        var type = new ComplexType<TestObject>();
        type.Field(d => d.someInt, nullable: true);
        schema.Query = type;
        schema.Initialize();

        type.Fields.Last().Type.ShouldBe(typeof(IntGraphType));
    }

    [Fact]
    public void infers_from_list_types()
    {
        var schema = new Schema();
        var type = new ComplexType<TestObject>();
        type.Field(d => d.someList, nullable: true);
        schema.Query = type;
        schema.Initialize();

        type.Fields.Last().Type.ShouldBe(typeof(ListGraphType<NonNullGraphType<IntGraphType>>));
    }

    [Fact]
    public void infers_field_description_from_expression()
    {
        var type = new ComplexType<TestObject>();
        _ = type.Field(d => d.someString);

        type.Fields.Last().Description.ShouldBe("Super secret");
    }

    [Fact]
    public void infers_field_deprecation_from_expression()
    {
        var type = new ComplexType<TestObject>();
        _ = type.Field(d => d.someBoolean);

        type.Fields.Last().DeprecationReason.ShouldBe("Use someInt");
    }

    [Fact]
    public void infers_field_default_from_expression()
    {
        var type = new ComplexType<TestObject>();
        _ = type.Field(d => d.someDate);

        type.Fields.Last().DefaultValue.ShouldBe(new DateTime(2019, 3, 14));
    }

    [Fact]
    public void throws_when_name_is_not_inferable()
    {
        var type = new ComplexType<Droid>();

        var exp = Should.Throw<ArgumentException>(() => type.Field(d => d.AppearsIn.First()));

        exp.Message.ShouldBe("Cannot infer a Field name from the expression: 'd.AppearsIn.First()' on parent GraphQL type: 'Droid'.");
    }

    [Fact]
    public void throws_when_type_is_not_inferable()
    {
        var type = new ComplexType<TestObject>();
        type.Field(d => d.valuePair);
        var schema = new Schema
        {
            Query = type
        };
        var exp = Should.Throw<InvalidOperationException>(() => schema.Initialize());
        exp.Message.ShouldStartWith($"The GraphQL type for field 'TestObject.valuePair' could not be derived implicitly. Could not find type mapping from CLR type '{typeof(KeyValuePair<int, string>).FullName}' to GraphType. Did you forget to register the type mapping with the 'ISchema.RegisterTypeMapping'?");
    }

    [Fact]
    public void throws_when_type_is_incompatible()
    {
        var type = new ComplexType<TestObject>();

        var exp = Should.Throw<ArgumentException>(() => type.Field(d => d.someInt));

        exp.InnerException.Message.ShouldStartWith("Explicitly nullable type: Nullable<Int32> cannot be coerced to a non nullable GraphQL type.");
    }

    [Fact]
    public void create_field_with_func_resolver()
    {
        var type = new ComplexType<Droid>();
        _ = type.Field<StringGraphType>("name", resolve: context => context.Source.Name);

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

    private static Exception test_field_name(string fieldName)
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

        exception.Message.ShouldStartWith($"A field name: '{fieldName}' must not begin with __, which is reserved by GraphQL introspection.");
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
        GlobalSwitches.NameValidation = (n, t) => { }; // disable "before" checks

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
            GlobalSwitches.NameValidation = NameValidator.ValidateDefault; // restore defaults
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
        var type = new ComplexType<TestObject>();
        var exception = Should.Throw<ArgumentOutOfRangeException>(() => type.Field<StringGraphType>(fieldName));

        exception.Message.ShouldStartWith("A field name can not be null or empty.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void throws_when_field_name_is_null_or_empty_using_field_builder(string fieldName)
    {
        var type = new ComplexType<TestObject>();
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
