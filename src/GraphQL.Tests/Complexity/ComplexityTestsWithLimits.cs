using Shouldly;
using Xunit;

namespace GraphQL.Tests.Complexity
{
    public class ComplexityTestsWithLimits : ComplexityTestBase
    {
        [Fact]
        public void simple_query_avec_limit()
        {
            var res = AnalyzeComplexity("query { A(first: 10) { B } }");
            res.TotalQueryDepth.ShouldBe(1);
            res.Complexity.ShouldBe(20);
        }

        [Fact]
        public void argument_ignored_when_no_subselection()
        {
            var res = AnalyzeComplexity("query { A(first: 10) }");
            res.TotalQueryDepth.ShouldBe(0);
            res.Complexity.ShouldBe(1);
        }

        [Fact]
        public void two_depth_query_A_limited()
        {
            var res = AnalyzeComplexity("query { A(id: \"iyIGiygiyg\") { B { C } } }");
            res.TotalQueryDepth.ShouldBe(2);
            res.Complexity.ShouldBe(5);
        }

        [Fact]
        public void two_depth_query_B_limited()
        {
            var res = AnalyzeComplexity("query { F A { B D(first: 3) { C E } } }");
            res.TotalQueryDepth.ShouldBe(2);
            res.Complexity.ShouldBe(23);
        }

        [Fact]
        public void last_value_ignored_when_first_exits()
        {
            var res = AnalyzeComplexity("query { F A { B D(last:100, first: 3) { C E } } }");
            res.TotalQueryDepth.ShouldBe(2);
            res.Complexity.ShouldBe(23);
        }

        [Fact]
        public void first_and_last_value_ignored_when_id_exists()
        {
            var res = AnalyzeComplexity("query { F A { B D(last:100, id:10, first: 3) { C } } }");
            res.TotalQueryDepth.ShouldBe(2);
            res.Complexity.ShouldBe(9);
        }

        [Fact]
        public void fragments_with_limits_handled()
        {
            var res = AnalyzeComplexity("{ F A { B ...X } } fragment X on Y { D(first: 3) { C E } }");
            res.TotalQueryDepth.ShouldBe(2);
            res.Complexity.ShouldBe(23);
        }
    }
}
