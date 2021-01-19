using System;
using GraphQL.Utilities;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Utilities
{
    public class NameValidatorTests
    {
        [Fact]
        public void ValidateName_whenNameIsNull_throwsArgumentOutOfRange() =>
           Should.Throw<ArgumentOutOfRangeException>(() => NameValidator.ValidateName(null));

        [Fact]
        public void ValidateName_whenNameIsEmpty_throwsArgumentOutOfRange() =>
            Should.Throw<ArgumentOutOfRangeException>(() => NameValidator.ValidateName(string.Empty));

        [Fact]
        public void ValidateName_whenNameIsWhitespace_throwsArgumentOutOfRange() =>
            Should.Throw<ArgumentOutOfRangeException>(() => NameValidator.ValidateName(" "));

        [Fact]
        public void ValidateName_whenNameStartsWithReservedCharacters_throwsArgumentOutOfRange() =>
            Should.Throw<ArgumentOutOfRangeException>(() => NameValidator.ValidateName("__dede"));

        [Theory]
        [InlineData("śćłó")]
        [InlineData("3test")]
        [InlineData("test Name")]
        public void ValidateName_whenNameContainsInvalidCharacters_throwsArgumentOutOfRange(string invalidName) =>
            Should.Throw<ArgumentOutOfRangeException>(() => NameValidator.ValidateName(invalidName));

        [Theory]
        [InlineData("goodName")]
        [InlineData("name3")]
        [InlineData("Test")]
        [InlineData("Test_Name")]
        [InlineData("_test")]
        public void ValidateName_whenNameIsCorrect_DoesntthrowsArgumentOutOfRange(string validName)
        {
            NameValidator.ValidateName(validName);
        }
    }
}
