using Shouldly;

using Xunit;

namespace GraphQL.Tests.PreciseComplexity
{
    public class PreciseComplexityTests : PreciseComplexityTestBase
    {
        [Fact]
        public void simple_scalar_field_query()
        {
            var query = "{string}";
            var result = this.Analyze(query);
            result.Complexity.ShouldBe(1d);
            result.MaxDepth.ShouldBe(1);
        }

        [Fact]
        public void simple_array_field_query()
        {
            var query = "{stringList}";
            var result = this.Analyze(query);
            result.Complexity.ShouldBe(11d); // 1 for request and 10 default for arrays
            result.MaxDepth.ShouldBe(1);
        }

        [Fact]
        public void simple_one_depth_query()
        {
            var query = "{this {string}}";
            var result = this.Analyze(query);
            result.Complexity.ShouldBe(2d);
            result.MaxDepth.ShouldBe(2);
        }

        [Fact]
        public void simple_one_depth_array_query()
        {
            var query = "{thisList {string}}";
            var result = this.Analyze(query);
            result.Complexity.ShouldBe(11d);
            result.MaxDepth.ShouldBe(2);
        }

        [Fact]
        public void simple_one_depth_array_with_array_query()
        {
            var query = "{thisList {stringList}}";
            var result = this.Analyze(query);
            result.Complexity.ShouldBe(1d + 10d * 11d);
            result.MaxDepth.ShouldBe(2);
        }

        [Fact]
        public void simple_two_depth_query()
        {
            var query = "{this {this {string}}}";
            var result = this.Analyze(query);
            result.Complexity.ShouldBe(3d);
            result.MaxDepth.ShouldBe(3);
        }
    }
}
