using System.Threading.Tasks;
using Shouldly;

namespace GraphQL.MicrosoftDI.Tests
{
    internal static class TestExtensions
    {
        public static void ShouldBeTask(this object value, object expected)
        {
            value.ShouldBeAssignableTo<Task<object>>().Result.ShouldBe(expected);
        }
    }
}
