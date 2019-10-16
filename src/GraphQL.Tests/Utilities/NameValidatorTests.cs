using Shouldly;
using System;
using Xunit;
using static GraphQL.Utilities.NameValidator;

namespace GraphQL.Tests.Utilities
{
    public class NameValidatorTests
    {
        [Fact]
        public void ValidateName_whenNameIsNull_throwsArgumentOutOfRange() =>
           Should.Throw<ArgumentOutOfRangeException>(() => ValidateName(null));

        [Fact]
        public void ValidateName_whenNameIsEmpty_throwsArgumentOutOfRange() =>
            Should.Throw<ArgumentOutOfRangeException>(() => ValidateName(string.Empty));

        [Fact]
        public void ValidateName_whenNameIsWhitespace_throwsArgumentOutOfRange() =>
            Should.Throw<ArgumentOutOfRangeException>(() => ValidateName(" "));

        [Fact]
        public void ValidateName_whenNameStartsWithReservedCharacters_throwsArgumentOutOfRange() =>
            Should.Throw<ArgumentOutOfRangeException>(() => ValidateName("__dede"));

        [Fact]
        public void ValidateName_whenNameContainsInvalidCharacters_throwsArgumentOutOfRange() =>
            Should.Throw<ArgumentOutOfRangeException>(() => ValidateName("śćłó"));

        [Fact]
        public void ValidateName_whenNameIsCorrect_DoesntthrowsArgumentOutOfRange()
        {
            var ex = Record.Exception(() => ValidateName("goodName"));
            ex.ShouldBeNull();
        }
    }
}
