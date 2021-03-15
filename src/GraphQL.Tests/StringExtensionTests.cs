using System;
using System.Collections.Generic;
using Shouldly;
using Xunit;

namespace GraphQL.Tests
{
    public class StringExtensionTests
    {
        [Theory]
        [InlineData("Ab", "AB")]
        [InlineData("ba", "BA")]
        [InlineData("AB", "AB")]
        [InlineData("aB", "A_B")]
        [InlineData("aBcD", "A_BC_D")]
        [InlineData("9A", "9_A")]
        [InlineData("99", "99")]
        [InlineData("9a", "9A")]
        [InlineData("A9", "A9")]
        [InlineData("a9", "A9")]
        [InlineData("ABc", "A_BC")]
        [InlineData("Abc", "ABC")]
        [InlineData("ABC", "ABC")]
        [InlineData("aBC", "A_BC")]
        [InlineData("TestABCHelloTest", "TEST_ABC_HELLO_TEST")]
        [InlineData("AbCDefGh", "AB_C_DEF_GH")]
        public void ToConstantCase(string input, string expected)
        {
            input.ToConstantCase().ShouldBe(expected);
        }

        [Theory]
        [InlineData(typeof(void), "Void")]
        [InlineData(typeof(int), "Int32")]
        [InlineData(typeof(Dictionary<string, bool>), "Dictionary<String,Boolean>")]
#if !NET48 //HACK: hangs test process after successfully compiling, prior to actually running the test
        [InlineData(typeof(List<Dictionary<string, HashSet<DateTime>>>), "List<Dictionary<String,HashSet<DateTime>>>")]
#endif
        [InlineData(typeof(Dictionary<,>), "Dictionary<TKey,TValue>")]
        [InlineData(typeof(List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<int>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>), "List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<Int32>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        public void GetFriendlyName_Should_Return_Expected_Results(Type source, string expected)
        {
            source.GetFriendlyName().ShouldBe(expected);
        }
    }
}
