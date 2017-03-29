using System.Collections.Generic;
using GraphQL.Tests.Complexity.CustomComplexityAnalyzer;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Complexity
{
    public class CustomComplexityAnalyzerTest : CustomComplexityAnalyzerTestBase
    {

        [Fact]
        public void should_work_when_complexity_within_params()
        {
            var query = @"
                query HeroNameQuery {
                  hero {
                    name
                  }
                }
            ";

            var complexityConfiguration = new ExtendedComplexityConfig { MaxComplexity = 105, complexityMap = new Dictionary<string, double>() { { "hero", 100f } } };
            var res = Execute(complexityConfiguration, query);

            res.Result.Errors.ShouldBe(null);
        }

        [Fact]
        public void should_not_work_when_complexity_within_params()
        {
            var query = @"
                query HeroNameQuery {
                  hero {
                    name
                  }
                }
            ";

            var complexityConfiguration = new ExtendedComplexityConfig
            {
                MaxComplexity = 100,
                complexityMap = new Dictionary<string, double>() { { "hero", 100f }, { "droid", 5f } }
            };
            var res = Execute(complexityConfiguration, query);

            res.Result.Errors.ShouldNotBe(null);
        }
    }
}
