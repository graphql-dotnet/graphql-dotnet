using GraphQL.Types;
using System;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class InvalidEnumGraphTypeTests
    {
        private enum Invalid
        {
            ćmaSobieLataPoŁadnymPoluWypełnionymRóżami = -1
        }

        class InvalidEnum : EnumerationGraphType<Invalid>
        {
            public InvalidEnum()
            {
                Name = "InvalidEnum";
            }
        }

        [Fact]
        public void AddValue_whenEnumContainsInvalidCharacters_shouldThrowArgumentException() =>
            Assert.Throws<ArgumentException>(() => new EnumerationGraphType<InvalidEnum>());
    }
}
