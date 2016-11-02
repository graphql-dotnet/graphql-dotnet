using Shouldly;
using Xunit;

namespace GraphQL.Tests.Complexity
{
    public class ComplexityBasicTests : ComplexityTestBase
    {
        [Fact]
        public void empty_query_complexity()
        {
            var res = AnalyzeComplexity("");
            res.ComplexityMap.Count.ShouldBe(0);
            res.Complexity.ShouldBe(0);
            res.TotalQueryDepth.ShouldBe(0);
        }

        [Fact]
        public void zero_depth_query()
        {
            var res = AnalyzeComplexity("query { A }");
            res.TotalQueryDepth.ShouldBe(0);
            res.Complexity.ShouldBe(2d);
        }

        [Fact]
        public void one_depth_query_A()
        {
            var res = AnalyzeComplexity("query { A { B } }");
            res.TotalQueryDepth.ShouldBe(1);
            res.Complexity.ShouldBe(6);
        }

        [Fact]
        public void one_depth_query_B()
        {
            var res = AnalyzeComplexity("query { A { B C D } }");
            res.TotalQueryDepth.ShouldBe(1);
            res.Complexity.ShouldBe(14);
        }

        [Fact]
        public void two_depth_query_A()
        {
            var res = AnalyzeComplexity("query { A { B { C } } }");
            res.TotalQueryDepth.ShouldBe(2);
            res.Complexity.ShouldBe(14);
        }

        [Fact]
        public void two_depth_query_B()
        {
            var res = AnalyzeComplexity("query { F A { B D { C E } } }");
            res.TotalQueryDepth.ShouldBe(2);
            res.Complexity.ShouldBe(28);
        }

        [Fact]
        public void three_depth_query()
        {
            var res = AnalyzeComplexity("query { A { B { C { D } } } }");
            res.TotalQueryDepth.ShouldBe(3);
            res.Complexity.ShouldBe(30);
        }
    }
}
