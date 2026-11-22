namespace GraphQL.Tests;

public class OptionalTests
{
    [Fact]
    public void UnspecifiedValue()
    {
        var optional = default(Optional<int>);

        optional.IsSpecified.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => optional.Value)
            .Message.ShouldBe("Optional object must have a value.");
        optional.GetValueOrDefault().ShouldBe(0);
        optional.GetValueOrDefault(42).ShouldBe(42);
        default(Optional<string>).GetValueOrDefault().ShouldBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    public void SpecifiedValue(int value)
    {
        var optional = new Optional<int>(value);

        optional.IsSpecified.ShouldBeTrue();
        optional.Value.ShouldBe(value);
        optional.GetValueOrDefault().ShouldBe(value);
        optional.GetValueOrDefault(-1).ShouldBe(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("value")]
    public void ImplicitConversion(string? value)
    {
        Optional<string?> optional = value;

        optional.IsSpecified.ShouldBeTrue();
        optional.Value.ShouldBe(value);
        optional.GetValueOrDefault().ShouldBe(value);
        optional.GetValueOrDefault("fallback").ShouldBe(value);
    }

    [Fact]
    public void SpecifiedNullableValue()
    {
        Optional<int?> optional = null;

        optional.IsSpecified.ShouldBeTrue();
        optional.Value.ShouldBeNull();
        optional.GetValueOrDefault().ShouldBeNull();
        optional.GetValueOrDefault(42).ShouldBeNull();
    }
}
