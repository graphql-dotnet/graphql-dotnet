using GraphQL.Types;
using Shouldly;
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

        [Fact]
        public void AddValue_whenEnumContainsInvalidCharacters_shouldThrowArgumentException() =>
            Should.Throw<ArgumentOutOfRangeException>(() => new EnumerationGraphType<Invalid>());
    }
}
