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
           Should.Throw<ArgumentOutOfRangeException>(() => NameValidator.ValidateName(null, NameType.Field));

        [Fact]
        public void ValidateName_whenNameIsEmpty_throwsArgumentOutOfRange() =>
            Should.Throw<ArgumentOutOfRangeException>(() => NameValidator.ValidateName(string.Empty, NameType.Field));

        [Fact]
        public void ValidateName_whenNameIsWhitespace_throwsArgumentOutOfRange() =>
            Should.Throw<ArgumentOutOfRangeException>(() => NameValidator.ValidateName(" ", NameType.Field));

        [Fact]
        public void ValidateName_whenNameStartsWithReservedCharacters_throwsArgumentOutOfRange() =>
            Should.Throw<ArgumentOutOfRangeException>(() => NameValidator.ValidateName("__dede", NameType.Field));

        [Theory]
        [InlineData("śćłó")]
        [InlineData("3test")]
        [InlineData("test Name")]
        public void ValidateName_whenNameContainsInvalidCharacters_throwsArgumentOutOfRange(string invalidName)
        {
            // race condition with does_not_throw_with_filtering_nameconverter test
            try
            {
                Should.Throw<ArgumentOutOfRangeException>(() => NameValidator.ValidateName(invalidName, NameType.Field));
            }
            catch (ShouldAssertException)
            {
                System.Threading.Thread.Sleep(100); // wait a bit and retry
                Should.Throw<ArgumentOutOfRangeException>(() => NameValidator.ValidateName(invalidName, NameType.Field));
            }
        }

        [Theory]
        [InlineData("goodName")]
        [InlineData("name3")]
        [InlineData("Test")]
        [InlineData("Test_Name")]
        [InlineData("_test")]
        public void ValidateName_whenNameIsCorrect_DoesntthrowsArgumentOutOfRange(string validName)
        {
            NameValidator.ValidateName(validName, NameType.Field);
        }
    }
}
