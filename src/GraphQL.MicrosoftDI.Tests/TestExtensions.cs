namespace GraphQL.MicrosoftDI.Tests;

internal static class TestExtensions
{
    public static void ShouldBeTask(this ValueTask<object> value, object expected)
    {
        value.AsTask().Result.ShouldBe(expected);
    }
}
