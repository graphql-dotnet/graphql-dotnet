using System.Threading.Tasks;
using Shouldly;

namespace GraphQL.MicrosoftDI.Tests
{
    internal static class TestExtensions
    {
        public static void ShouldBeTask(this object value, object expected)
        {
            value.ShouldBeAssignableTo(typeof(Task<object>));
            ((Task<object>)value).Result.ShouldBe(expected);
        }
    }
}
