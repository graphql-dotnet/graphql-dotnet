namespace GraphQL.Tests.Attributes;

public class OptionalTypeAttributeTests
{
    [Theory]
    [InlineData(typeof(CustomOptional<>))]
    [InlineData(typeof(CustomOptionalClass<>))]
    public void accepts_optional_wrappers(Type optionalType)
    {
        new OptionalTypeAttribute(optionalType).OptionalType.ShouldBe(optionalType);
    }

    [Fact]
    public void rejects_null()
    {
        Should.Throw<ArgumentNullException>(() => new OptionalTypeAttribute(null!))
            .ParamName.ShouldBe("optionalType");
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(CustomOptional<string>))]
    [InlineData(typeof(Dictionary<,>))]
    [InlineData(typeof(IEnumerable<>))]
    [InlineData(typeof(AbstractOptional<>))]
    [InlineData(typeof(NoParameterlessConstructor<>))]
    [InlineData(typeof(List<>))]
    [InlineData(typeof(PrivateValueConstructor<>))]
    [InlineData(typeof(ObjectValueConstructor<>))]
    public void rejects_invalid_optional_types(Type optionalType)
    {
        Should.Throw<ArgumentException>(() => new OptionalTypeAttribute(optionalType))
            .ParamName.ShouldBe("optionalType");
    }

    private abstract class AbstractOptional<T>
    {
        public AbstractOptional() { }
        public AbstractOptional(T value) { }
    }

    private class NoParameterlessConstructor<T>
    {
        public NoParameterlessConstructor(T value) { }
    }

    private class PrivateValueConstructor<T>
    {
        public PrivateValueConstructor() { }
        private PrivateValueConstructor(T value) { }
    }

    private class ObjectValueConstructor<T>
    {
        public ObjectValueConstructor() { }
        public ObjectValueConstructor(object value) { }
    }
}
