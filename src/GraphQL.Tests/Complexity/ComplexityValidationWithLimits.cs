using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Validation.Complexity;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Complexity
{
    public class ComplexityValidationWithLimits : ComplexityTestBase
    {
        [Fact]
        public void two_depth_query_A_limited()
        {
            var res = AnalyzeComplexity("query { A(id: \"iyIGiygiyg\") { B { C } } }");
            res.TotalQueryDepth.ShouldBe(2);
            res.Complexity.ShouldBe(7);
        }

        [Fact]
        public void two_depth_query_B_limited()
        {
            var res = AnalyzeComplexity("query { F A { B D(first: 3) { C E } } }");
            res.TotalQueryDepth.ShouldBe(2);
            res.Complexity.ShouldBe(38);
        }
    }
}
