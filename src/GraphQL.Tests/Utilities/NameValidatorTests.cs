using System;
using Xunit;
using static GraphQL.Utilities.NameValidator;

namespace GraphQL.Tests.Utilities
{
    public class NameValidatorTests
    {
        [Fact]
        public void ValidateName_whenNameIsEmpty_throwsArgumentOutOfRange() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => ValidateName(string.Empty));

        [Fact]
        public void ValidateName_whenNameStartsWithReservedCharacters_throwsArgumentOutOfRange() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => ValidateName("__dede"));

        [Fact]
        public void ValidateName_whenNameContainsInvalidCharacters_throwsArgumentOutOfRange() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => ValidateName("śćłó"));

        [Fact]
        public void ValidateName_whenNameIsCorrect_DoesntthrowsArgumentOutOfRange()
        {
            var ex = Record.Exception(() => ValidateName("goodName"));
            Assert.Null(ex);
        }
    }
}
