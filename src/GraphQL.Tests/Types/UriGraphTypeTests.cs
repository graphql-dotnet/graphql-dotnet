using GraphQL.Types;

namespace GraphQL.Tests.Types;

public class UriGraphTypeTests
{
    private readonly UriGraphType uriGraphType = new UriGraphType();

    [Fact]
    public void ParseValue_uriIsAString_ReturnValidUriGraphType() =>
        uriGraphType.ParseValue("http://www.wp.pl").ShouldBe(new Uri("http://www.wp.pl"));

    [Fact]
    public void ParseValue_uriIsAStringWithHttps_ReturnValidUriGraphType() =>
        uriGraphType.ParseValue("https://www.wp.pl").ShouldBe(new Uri("https://www.wp.pl"));

    [Fact]
    public void ParseValue_uriIsAUri_ReturnValidUriGraphType() =>
        uriGraphType.ParseValue(new Uri("https://www.wp.pl")).ShouldBe(new Uri("https://www.wp.pl"));

    [Fact]
    public void ParseValue_stringAsInvalidUri_ThrowsFormatException() =>
        Should.Throw<UriFormatException>(() => uriGraphType.ParseValue("www.wp.pl"));

    [Fact]
    public void Serialize_uriIsAString_ReturnValidUriGraphType() =>
        uriGraphType.Serialize("https://www.wp.pl").ShouldBe(new Uri("https://www.wp.pl"));

    [Fact]
    public void Serialize_uriIsAUri_ReturnValidUriGraphType() =>
        uriGraphType.Serialize(new Uri("https://www.wp.pl")).ShouldBe(new Uri("https://www.wp.pl"));

    [Fact]
    public void Serialize_stringAsInvalidUri_ThrowsFormatException() =>
        Should.Throw<UriFormatException>(() => uriGraphType.Serialize("www.wp.pl"));

    [Fact]
    public void Serialize_uriWithSpecialCharacters_ReturnValidUriGraphType() =>
        uriGraphType.Serialize(new Uri("https://example.com/foo%20bar")).ShouldBe("https://example.com/foo%20bar");

    [Fact]
    public void Serialize_relativeUriWithSpecialCharacters_ReturnValidUriGraphType() =>
        uriGraphType.Serialize(new Uri("/foo%20bar", UriKind.Relative)).ShouldBe("/foo%20bar");

    [Fact]
    public void Serialize_uriIsNormalized_ReturnValidUriGraphType() =>
        uriGraphType.Serialize(new Uri("HTTPS://example.com")).ShouldBe("https://example.com/");
}
