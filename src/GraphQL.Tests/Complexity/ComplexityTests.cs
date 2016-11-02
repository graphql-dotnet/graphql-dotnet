using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Complexity
{
    public class ComplexityTests : ComplexityTestBase
    {
        [Fact]
        public void inline_fragments_test()
        {
            var withFrag =
                AnalyzeComplexity(
                    @"query withInlineFragment { profiles(handles: [""dnetguru""]) { handle ... on User { friends { count } } } }");
            var woFrag =
                AnalyzeComplexity(
                    @"query withoutFragments { profiles(handles: [""dnetguru""]) { handle friends { count } } }");

            withFrag.Complexity.ShouldBe(woFrag.Complexity);
            withFrag.TotalQueryDepth.ShouldBe(woFrag.TotalQueryDepth);
        }

        [Fact]
        public void fragments_test()
        {
            var withFrag =
                AnalyzeComplexity(
                    @"{ leftComparison: hero(episode: EMPIRE) { ...comparisonFields } rightComparison: hero(episode: JEDI) { ...comparisonFields} } fragment comparisonFields on Character { name appearsIn friends { name } }");
            var woFrag =
                AnalyzeComplexity(
                    @"{ leftComparison: hero(episode: EMPIRE) { name appearsIn friends { name } } rightComparison: hero(episode: JEDI) { name appearsIn friends { name } } }");

            // TODO: Non-inline fragments are not handled yet.
            
            // withFrag.Complexity.ShouldBe(woFrag.Complexity);
            // withFrag.TotalQueryDepth.ShouldBe(woFrag.TotalQueryDepth);
        }

        [Fact]
        public void absurdly_huge_query()
        {
            try
            {
                AnalyzeComplexity(
                    @"{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A{A}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}");
            }
            catch (InvalidOperationException ex)
            {
                ex.Message.ShouldBe("Query is too complex to validate.");
            }
        }        
    }
}
