using GraphQL.Types;

namespace GraphQL.Tests.Attributes;

public class BaseGraphTypeAttributeTests
{
    [Theory]
    [InlineData(nameof(TestClass.Value), typeof(CustomInputGraphType))]
    [InlineData(nameof(TestClass.NullableValue), typeof(CustomInputGraphType))]
    [InlineData(nameof(TestClass.NonNullValue), typeof(NonNullGraphType<CustomInputGraphType>))]
    [InlineData(nameof(TestClass.ListValue), typeof(ListGraphType<NonNullGraphType<CustomInputGraphType>>))]
    [InlineData(nameof(TestClass.NonNullListValue), typeof(NonNullGraphType<ListGraphType<NonNullGraphType<CustomInputGraphType>>>))]
    [InlineData(nameof(TestClass.ListOfNullableValue), typeof(NonNullGraphType<ListGraphType<CustomInputGraphType>>))]
    [InlineData(nameof(TestClass.NullableListOfNullableValue), typeof(ListGraphType<CustomInputGraphType>))]
    public void InputBaseTypeAttribute_SetsGraphType_ForInputType(string propertyName, Type expectedType)
    {
        var typeInfo = new TypeInformation(typeof(TestClass).GetProperty(propertyName)!, true);
        var attr = new InputBaseTypeAttribute(typeof(CustomInputGraphType));

        attr.Modify(typeInfo);

        typeInfo.ConstructGraphType().ShouldBe(expectedType);
    }

    [Theory]
    [InlineData(nameof(TestClass.Value))]
    [InlineData(nameof(TestClass.NullableValue))]
    public void InputBaseTypeAttribute_DoesNotSetGraphType_ForOutputType(string propertyName)
    {
        var typeInfo = new TypeInformation(typeof(TestClass).GetProperty(propertyName)!, false);
        var originalGraphType = typeInfo.ConstructGraphType();
        var attr = new InputBaseTypeAttribute(typeof(CustomInputGraphType));

        attr.Modify(typeInfo);

        typeInfo.ConstructGraphType().ShouldBe(originalGraphType);
    }

    [Theory]
    [InlineData(nameof(TestClass.Value), typeof(CustomInputGraphType))]
    [InlineData(nameof(TestClass.NullableValue), typeof(CustomInputGraphType))]
    [InlineData(nameof(TestClass.NonNullValue), typeof(NonNullGraphType<CustomInputGraphType>))]
    public void InputBaseTypeAttribute_Generic_SetsGraphType_ForInputType(string propertyName, Type expectedType)
    {
        var typeInfo = new TypeInformation(typeof(TestClass).GetProperty(propertyName)!, true);
        var attr = new InputBaseTypeAttribute<CustomInputGraphType>();

        attr.Modify(typeInfo);

        typeInfo.ConstructGraphType().ShouldBe(expectedType);
    }

    [Theory]
    [InlineData(nameof(TestClass.Value), typeof(CustomOutputGraphType))]
    [InlineData(nameof(TestClass.NullableValue), typeof(CustomOutputGraphType))]
    [InlineData(nameof(TestClass.NonNullValue), typeof(NonNullGraphType<CustomOutputGraphType>))]
    [InlineData(nameof(TestClass.ListValue), typeof(ListGraphType<NonNullGraphType<CustomOutputGraphType>>))]
    [InlineData(nameof(TestClass.NonNullListValue), typeof(NonNullGraphType<ListGraphType<NonNullGraphType<CustomOutputGraphType>>>))]
    [InlineData(nameof(TestClass.ListOfNullableValue), typeof(NonNullGraphType<ListGraphType<CustomOutputGraphType>>))]
    [InlineData(nameof(TestClass.NullableListOfNullableValue), typeof(ListGraphType<CustomOutputGraphType>))]
    public void OutputBaseTypeAttribute_SetsGraphType_ForOutputType(string propertyName, Type expectedType)
    {
        var typeInfo = new TypeInformation(typeof(TestClass).GetProperty(propertyName)!, false);
        var attr = new OutputBaseTypeAttribute(typeof(CustomOutputGraphType));

        attr.Modify(typeInfo);

        typeInfo.ConstructGraphType().ShouldBe(expectedType);
    }

    [Theory]
    [InlineData(nameof(TestClass.Value))]
    [InlineData(nameof(TestClass.NullableValue))]
    public void OutputBaseTypeAttribute_DoesNotSetGraphType_ForInputType(string propertyName)
    {
        var typeInfo = new TypeInformation(typeof(TestClass).GetProperty(propertyName)!, true);
        var originalGraphType = typeInfo.ConstructGraphType();
        var attr = new OutputBaseTypeAttribute(typeof(CustomOutputGraphType));

        attr.Modify(typeInfo);

        typeInfo.ConstructGraphType().ShouldBe(originalGraphType);
    }

    [Theory]
    [InlineData(nameof(TestClass.Value), typeof(CustomOutputGraphType))]
    [InlineData(nameof(TestClass.NullableValue), typeof(CustomOutputGraphType))]
    [InlineData(nameof(TestClass.NonNullValue), typeof(NonNullGraphType<CustomOutputGraphType>))]
    public void OutputBaseTypeAttribute_Generic_SetsGraphType_ForOutputType(string propertyName, Type expectedType)
    {
        var typeInfo = new TypeInformation(typeof(TestClass).GetProperty(propertyName)!, false);
        var attr = new OutputBaseTypeAttribute<CustomOutputGraphType>();

        attr.Modify(typeInfo);

        typeInfo.ConstructGraphType().ShouldBe(expectedType);
    }

    [Theory]
    [InlineData(nameof(TestClass.Value), true, typeof(IdGraphType))]
    [InlineData(nameof(TestClass.NullableValue), true, typeof(IdGraphType))]
    [InlineData(nameof(TestClass.NonNullValue), true, typeof(NonNullGraphType<IdGraphType>))]
    [InlineData(nameof(TestClass.ListValue), true, typeof(ListGraphType<NonNullGraphType<IdGraphType>>))]
    [InlineData(nameof(TestClass.NonNullListValue), true, typeof(NonNullGraphType<ListGraphType<NonNullGraphType<IdGraphType>>>))]
    [InlineData(nameof(TestClass.ListOfNullableValue), true, typeof(NonNullGraphType<ListGraphType<IdGraphType>>))]
    [InlineData(nameof(TestClass.NullableListOfNullableValue), true, typeof(ListGraphType<IdGraphType>))]
    [InlineData(nameof(TestClass.Value), false, typeof(IdGraphType))]
    [InlineData(nameof(TestClass.NullableValue), false, typeof(IdGraphType))]
    [InlineData(nameof(TestClass.NonNullValue), false, typeof(NonNullGraphType<IdGraphType>))]
    [InlineData(nameof(TestClass.ListValue), false, typeof(ListGraphType<NonNullGraphType<IdGraphType>>))]
    [InlineData(nameof(TestClass.NonNullListValue), false, typeof(NonNullGraphType<ListGraphType<NonNullGraphType<IdGraphType>>>))]
    [InlineData(nameof(TestClass.ListOfNullableValue), false, typeof(NonNullGraphType<ListGraphType<IdGraphType>>))]
    [InlineData(nameof(TestClass.NullableListOfNullableValue), false, typeof(ListGraphType<IdGraphType>))]
    public void BaseGraphTypeAttribute_SetsGraphType(string propertyName, bool isInputType, Type expectedType)
    {
        var typeInfo = new TypeInformation(typeof(TestClass).GetProperty(propertyName)!, isInputType);
        var attr = new BaseGraphTypeAttribute(typeof(IdGraphType));

        attr.Modify(typeInfo);

        typeInfo.ConstructGraphType().ShouldBe(expectedType);
    }

    [Theory]
    [InlineData(nameof(TestClass.Value), true, typeof(IdGraphType))]
    [InlineData(nameof(TestClass.NullableValue), true, typeof(IdGraphType))]
    [InlineData(nameof(TestClass.NonNullValue), true, typeof(NonNullGraphType<IdGraphType>))]
    [InlineData(nameof(TestClass.Value), false, typeof(IdGraphType))]
    [InlineData(nameof(TestClass.NullableValue), false, typeof(IdGraphType))]
    [InlineData(nameof(TestClass.NonNullValue), false, typeof(NonNullGraphType<IdGraphType>))]
    public void BaseGraphTypeAttribute_Generic_SetsGraphType(string propertyName, bool isInputType, Type expectedType)
    {
        var typeInfo = new TypeInformation(typeof(TestClass).GetProperty(propertyName)!, isInputType);
        var attr = new BaseGraphTypeAttribute<IdGraphType>();

        attr.Modify(typeInfo);

        typeInfo.ConstructGraphType().ShouldBe(expectedType);
    }

    [Fact]
    public void InputBaseTypeAttribute_ThrowsForNullType()
    {
        var attr = new InputBaseTypeAttribute(typeof(CustomInputGraphType));
        Should.Throw<ArgumentNullException>(() => attr.InputBaseType = null!);
    }

    [Fact]
    public void InputBaseTypeAttribute_ThrowsForNonInputType()
    {
        var attr = new InputBaseTypeAttribute(typeof(CustomInputGraphType));
        var ex = Should.Throw<ArgumentException>(() => attr.InputBaseType = typeof(CustomOutputGraphType));
        ex.Message.ShouldContain("should be an input type");
    }

    [Fact]
    public void OutputBaseTypeAttribute_ThrowsForNullType()
    {
        var attr = new OutputBaseTypeAttribute(typeof(CustomOutputGraphType));
        Should.Throw<ArgumentNullException>(() => attr.OutputBaseType = null!);
    }

    [Fact]
    public void OutputBaseTypeAttribute_ThrowsForNonOutputType()
    {
        var attr = new OutputBaseTypeAttribute(typeof(CustomOutputGraphType));
        var ex = Should.Throw<ArgumentException>(() => attr.OutputBaseType = typeof(CustomInputGraphType));
        ex.Message.ShouldContain("should be an output type");
    }

    [Fact]
    public void BaseGraphTypeAttribute_ThrowsForNullType()
    {
        var attr = new BaseGraphTypeAttribute(typeof(IdGraphType));
        Should.Throw<ArgumentNullException>(() => attr.BaseGraphType = null!);
    }

    [Fact]
    public void BaseGraphTypeAttribute_ThrowsForNonGraphType()
    {
        var attr = new BaseGraphTypeAttribute(typeof(IdGraphType));
        var ex = Should.Throw<ArgumentException>(() => attr.BaseGraphType = typeof(string));
        ex.Message.ShouldContain("should be a graph type");
    }

    [Fact]
    public void IdAttribute_InheritsFromBaseGraphType()
    {
        var attr = new IdAttribute();
        attr.ShouldBeAssignableTo<BaseGraphTypeAttribute<IdGraphType>>();
    }

    [Theory]
    [InlineData(nameof(TestClass.Value), typeof(IdGraphType))]
    [InlineData(nameof(TestClass.NullableValue), typeof(IdGraphType))]
    [InlineData(nameof(TestClass.NonNullValue), typeof(NonNullGraphType<IdGraphType>))]
    public void IdAttribute_SetsGraphTypeToIdGraphType(string propertyName, Type expectedType)
    {
        var typeInfo = new TypeInformation(typeof(TestClass).GetProperty(propertyName)!, false);
        var attr = new IdAttribute();

        attr.Modify(typeInfo);

        typeInfo.ConstructGraphType().ShouldBe(expectedType);
    }

    private class TestClass
    {
        public string? Value { get; set; }
        public string? NullableValue { get; set; }
        public string NonNullValue { get; set; } = "";
        public List<string>? ListValue { get; set; }
        public List<string> NonNullListValue { get; set; } = new();
        public List<string?> ListOfNullableValue { get; set; } = new();
        public List<string?>? NullableListOfNullableValue { get; set; }
    }

    private class CustomInputGraphType : InputObjectGraphType
    {
    }

    private class CustomOutputGraphType : ObjectGraphType
    {
    }
}
