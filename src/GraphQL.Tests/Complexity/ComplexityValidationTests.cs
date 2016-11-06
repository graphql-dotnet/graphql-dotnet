using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Validation.Complexity;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Complexity
{
    public class ComplexityValidationTest : ComplexityTestBase
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

            var complexityConfiguration = new ComplexityConfiguration { FieldImpact = 2, MaxComplexity = 6, MaxDepth = 1 };
            var res = Execute(complexityConfiguration, query);

            res.Result.Errors.ShouldBe(null);
        }

        [Fact]
        public void error_when_too_nested()
        {
            var query = @"
                query FriendsOfFriends {
                  hero {
                    friends
                    {
                      friends
                      {
                        id
                        name
                      }
                    }
                  }
                }";

            var complexityConfiguration = new ComplexityConfiguration { MaxDepth = 2 };
            var res = Execute(complexityConfiguration, query);

            res.Result.Errors.ShouldNotBe(null);
            res.Result.Errors.Count.ShouldBe(1);
            res.Result.Errors.First().InnerException?.GetType().ShouldBe(typeof(InvalidOperationException));
        }

        [Fact]
        public void fail_when_too_complex()
        {
            var query = @"
                query BasicQuery {
                  hero {
                    id
                    name
                    appearsIn
                  }
                }";

            var complexityConfiguration = new ComplexityConfiguration { FieldImpact = 5, MaxComplexity = 10 };
            var res = Execute(complexityConfiguration, query);

            res.Result.Errors.ShouldNotBe(null);
            res.Result.Errors.Count.ShouldBe(1);
            res.Result.Errors.First().InnerException?.GetType().ShouldBe(typeof(InvalidOperationException));
        }

        [Fact]
        public void fail_when_too_complex_and_nested()
        {
            var query = @"
                query FriendsOfFriends {
                  hero {
                    friends
                    {
                      id
                      name
                      appearsIn
                      friends
                      {
                        id
                        name
                      }
                    }
                  }
                }";

            var complexityConfiguration = new ComplexityConfiguration
            {
                FieldImpact = 5,
                MaxComplexity = 25,
                MaxDepth = 2
            };
            var res = Execute(complexityConfiguration, query);

            res.Result.Errors.ShouldNotBe(null);
            res.Result.Errors.Count.ShouldBe(1);
            res.Result.Errors.First().InnerException?.GetType().ShouldBe(typeof(InvalidOperationException));
        }
    }
}