using GraphQL.Types;
using Shouldly;
using Xunit;
using System;

namespace GraphQL.Tests.Types
{
    public class UriGraphTypeTests
    {
        private UriGraphType uriGraphType = new UriGraphType();

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
            Assert.Throws<UriFormatException>(() => uriGraphType.ParseValue("www.wp.pl"));
    }
}
