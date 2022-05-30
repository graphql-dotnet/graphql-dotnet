using GraphQL.Types;

namespace GraphQL.Tests.Types;

public class InvalidEnumGraphTypeTests
{
    private enum Invalid
    {
        ćmaSobieLataPoŁadnymPoluWypełnionymRóżami = -1
    }

    [Fact]
    public void AddValue_whenEnumContainsInvalidCharacters_shouldThrowArgumentException()
    {
        // race condition with does_not_throw_with_filtering_nameconverter test
        try
        {
            Should.Throw<ArgumentOutOfRangeException>(() => new EnumerationGraphType<Invalid>());
        }
        catch (ShouldAssertException)
        {
            System.Threading.Thread.Sleep(100); // wait a bit and retry
            Should.Throw<ArgumentOutOfRangeException>(() => new EnumerationGraphType<Invalid>());
        }
    }
}
