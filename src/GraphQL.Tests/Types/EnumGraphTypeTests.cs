using System.ComponentModel;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Types;

public class EnumGraphTypeTests
{
    [Description("The best colors ever!")]
    [Obsolete("Just some reason")]
    private enum Colors
    {
        Red = 1,
        Blue,
        Green,

        [Obsolete("No more yellow")]
        Yellow,

        [Description("A pale purple color named after the mallow flower.")]
        Mauve
    }

    private class ColorEnum : EnumerationGraphType<Colors>
    {
        public ColorEnum()
        {
            Name = "ColorsEnum";
        }
    }

    private readonly EnumerationGraphType<Colors> type = new();

    [Fact]
    public void adds_values_from_enum()
    {
        type.Values.Count.ShouldBe(5);
        type.Values.First().Name.ShouldBe("RED");
    }

    [Fact]
    public void adds_values_from_enum_no_description_attribute()
    {
        type.Values.Count.ShouldBe(5);
        type.Values.First().Description.ShouldBeNull();
    }

    [Fact]
    public void adds_values_from_enum_with_description_attribute()
    {
        type.Values.Count.ShouldBe(5);
        type.Values.Last().Description.ShouldBe("A pale purple color named after the mallow flower.");
    }

    [Fact]
    public void adds_values_from_enum_with_obsolete_attribute()
    {
        type.Values.Count.ShouldBe(5);
        type.Values["YELLOW"].DeprecationReason.ShouldBe("No more yellow");
    }

    [Fact]
    public void description_and_obsolete_from_enum()
    {
        type.Description.ShouldBe("The best colors ever!");
        type.DeprecationReason.ShouldBe("Just some reason");
    }

    [Fact]
    public void adds_values_from_enum_custom_casing_should_throw()
    {
        Should.Throw<InvalidOperationException>(() => type.ParseValue("rED")).Message.ShouldBe("Unable to convert 'rED' to the scalar type 'Colors'");
    }

    [Fact]
    public void infers_name()
    {
        type.Name.ShouldBe("Colors");
    }

    [Fact]
    public void leaves_name_alone()
    {
        var otherType = new ColorEnum();

        otherType.Name.ShouldBe("ColorsEnum");
        otherType.Values.Count.ShouldBe(5);
    }

    [Fact]
    public void parses_from_name()
    {
        type.ParseValue("RED").ShouldBe(Colors.Red);
    }

    [Fact]
    public void parse_value_is_null_safe()
    {
        type.ParseValue(null).ShouldBe(null);
    }

    [Fact]
    public void parse_native_clr_enum_value()
    {
        type.CanParseValue(Colors.Red).ShouldBeTrue();
        type.ParseValue(Colors.Red).ShouldBe(Colors.Red);
    }

    [Fact]
    public void parse_native_clr_enum_value_when_not_defined()
    {
        type.CanParseValue((Colors)100500).ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => type.ParseValue((Colors)100500)).Message.ShouldBe("Unable to convert '100500' to the scalar type 'Colors'");
    }

    [Fact]
    public void does_not_allow_nulls_to_be_added()
    {
        Assert.Throws<ArgumentNullException>(() => new EnumerationGraphType().Add(null));
    }

    [Fact]
    public void parse_literal_from_name()
    {
        type.ParseLiteral(new GraphQLEnumValue { Name = new GraphQLName("RED") }).ShouldBe(Colors.Red);
    }

    [Fact]
    public void serialize_by_value()
    {
        type.Serialize(Colors.Red).ShouldBe("RED");
    }

    [Fact]
    public void serialize_by_underlying_value()
    {
        type.Serialize((int)Colors.Red).ShouldBe("RED");
    }

    [Fact]
    public void serialize_by_name_throws()
    {
        Should.Throw<InvalidOperationException>(() => type.Serialize("RED"));
    }

    [Fact]
    public void serialize_should_work_with_null_values()
    {
        var en = new EnumerationGraphType();
        en.Add("one", 100500);
        en.Add("two", null);

        en.Serialize(100500).ShouldBe("one");
        en.Serialize(null).ShouldBe("two");
    }

    [Fact]
    public void toast_should_work_with_null_values()
    {
        var en = new EnumerationGraphType();
        en.Add("one", 100500);
        en.Add("two", null);

        en.ToAST(100500).ShouldBeOfType<GraphQLEnumValue>().Name.Value.ShouldBe("one");
        en.ToAST(null).ShouldBeOfType<GraphQLEnumValue>().Name.Value.ShouldBe("two");
    }

    [Fact]
    public void toAST_returns_enum_value()
    {
        type.ToAST(Colors.Red)
            .ShouldNotBeNull()
            .ShouldBeOfType<GraphQLEnumValue>()
            .Name.Value.ShouldBe("RED");
    }

    [Fact]
    public void to_constant_case_tests()
    {
        var e = new EnumerationGraphType<MyEnum>();
        e.Values.Count.ShouldBe(2);
        e.Values.FindByValue(MyEnum.TestHello).Name.ShouldBe("TEST_HELLO");
        e.Values.FindByValue(MyEnum.Hello1).Name.ShouldBe("HELLO_1");
    }

    private enum MyEnum
    {
        TestHello,
        Hello1
    }

    [Fact]
    public void enum_names_from_attribute_tests()
    {
        EnumerationGraphType e = new EnumerationGraphType<ConstantCaseEnum>();
        e.Values.Count.ShouldBe(2);
        e.Values.FindByValue(ConstantCaseEnum.OneOne).Name.ShouldBe("ONE_ONE");
        e.Values.FindByValue(ConstantCaseEnum.TwoTwo).Name.ShouldBe("TWO_TWO");

        e = new EnumerationGraphType<CamelCaseEnum>();
        e.Values.Count.ShouldBe(2);
        e.Values.FindByValue(CamelCaseEnum.OneOne).Name.ShouldBe("oneOne");
        e.Values.FindByValue(CamelCaseEnum.TwoTwo).Name.ShouldBe("twoTwo");

        e = new EnumerationGraphType<PascalCaseEnum>();
        e.Values.Count.ShouldBe(2);
        e.Values.FindByValue(PascalCaseEnum.OneOne).Name.ShouldBe("OneOne");
        e.Values.FindByValue(PascalCaseEnum.TwoTwo).Name.ShouldBe("TwoTwo");
    }

    [ConstantCase]
    private enum ConstantCaseEnum
    {
        OneOne,
        TwoTwo
    }

    [CamelCase]
    private enum CamelCaseEnum
    {
        OneOne,
        TwoTwo
    }

    [PascalCase]
    private enum PascalCaseEnum
    {
        OneOne,
        TwoTwo
    }

    [Fact]
    public void respects_attributes()
    {
        var test = new EnumerationGraphType<EnumAttributeTest>();
        test.Name.ShouldBe("EnumTest");
        test.Description.ShouldBe("Test description");
        test.DeprecationReason.ShouldBe("Test obsolete");
        test.GetMetadata<string>("Key1").ShouldBe("Value1");
        test.Values.Count.ShouldBe(2);
        var value1 = test.Values.FindByName("CUSTOM_NAME").ShouldNotBeNull();
        value1.Value.ShouldBe(EnumAttributeTest.Enum1);
        value1.Description.ShouldBe("Custom enum value");
        var value2 = test.Values.FindByName("ENUM_2").ShouldNotBeNull();
        value2.Value.ShouldBe(EnumAttributeTest.Enum2);
        value2.GetMetadata<string>("Key2").ShouldBe("Value2");
        test.Values.FindByName("IGNORED").ShouldBeNull();
    }

    [Name("EnumTest")]
    [Description("Test description")]
    [Obsolete("Test obsolete")]
    [Metadata("Key1", "Value1")]
    private enum EnumAttributeTest
    {
        [Ignore]
        Ignored,
        [Name("CUSTOM_NAME")]
        [Description("Custom enum value")]
        Enum1,
        [Metadata("Key2", "Value2")]
        [Obsolete("Deprecated enum value")]
        Enum2,
    }
}
