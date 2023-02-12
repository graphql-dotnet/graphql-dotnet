using GraphQL.Execution;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using GraphQL.Validation.Errors;
using GraphQL.Validation.Errors.Custom;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Complexity;

public class ComplexityValidationTest : ComplexityTestBase
{
    [Fact]
    public void should_work_when_complexity_within_params()
    {
        const string query = """
            query HeroNameQuery {
              hero {
              name
              }
            }
            """;

        var complexityConfiguration = new ComplexityConfiguration { FieldImpact = 2, MaxComplexity = 6, MaxDepth = 1 };
        var res = Execute(complexityConfiguration, query);

        res.Result.Errors.ShouldBe(null);
    }

    [Fact]
    public void error_when_too_nested()
    {
        const string query = """
            query FriendsOfFriends {
              hero {
              friends {
                friends {
                  id
                  name
                  }
                }
              }
            }
            """;

        var complexityConfiguration = new ComplexityConfiguration { MaxDepth = 2 };
        var res = Execute(complexityConfiguration, query);

        res.Result.Errors.ShouldNotBe(null);
        res.Result.Errors.Count.ShouldBe(1);
        res.Result.Errors[0].ShouldBeOfType<ComplexityError>();
        res.Result.Errors[0].Message.ShouldBe("Query is too nested to execute. Depth is 3 levels, maximum allowed on this endpoint is 2.");
        res.Result.Errors[0].InnerException.ShouldBeNull();
    }

    [Fact]
    public void fail_when_too_complex()
    {
        const string query = """
            query BasicQuery {
              hero {
              id
              name
              appearsIn
              }
            }
            """;

        var complexityConfiguration = new ComplexityConfiguration { FieldImpact = 5, MaxComplexity = 10 };
        var res = Execute(complexityConfiguration, query);

        res.Result.Errors.ShouldNotBe(null);
        res.Result.Errors.Count.ShouldBe(1);
        res.Result.Errors[0].ShouldBeOfType<ComplexityError>();
        res.Result.Errors[0].Message.ShouldBe("Query is too complex to execute. Complexity is 20, maximum allowed on this endpoint is 10. The field with the highest complexity is 'hero' with value 5.");
        res.Result.Errors[0].InnerException.ShouldBeNull();
    }

    [Fact]
    public void fail_when_too_complex_and_nested()
    {
        const string query = """
            query FriendsOfFriends {
              hero {
                friends {
                  id
                  name
                  appearsIn
                  friends {
                    id
                    name
                  }
                }
              }
            }
            """;

        var complexityConfiguration = new ComplexityConfiguration
        {
            FieldImpact = 5,
            MaxComplexity = 25,
            MaxDepth = 2
        };
        var res = Execute(complexityConfiguration, query);

        res.Result.Errors.ShouldNotBe(null);
        res.Result.Errors.Count.ShouldBe(1);
        res.Result.Errors[0].ShouldBeOfType<ComplexityError>();
        res.Result.Errors[0].Message.ShouldBe("Query is too complex to execute. Complexity is 480, maximum allowed on this endpoint is 25. The field with the highest complexity is 'friends' with value 125.");
        res.Result.Errors[0].InnerException.ShouldBeNull();
    }

    // https://github.com/graphql-dotnet/graphql-dotnet/issues/3527
    [Fact]
    public void recursive_fragment_should_not_end_in_eternal_loop1()
    {
        const string query = """
            {
                type_All(limit: 20) {
                    items {
                        links(limit: 20) {
                            items {
                                ... RecursiveFragment
                            }
                        }
                    }
                }
            }
            fragment RecursiveFragment on Type   {
               links(limit: 20) {
                    items {
                        ... RecursiveFragment
                    }
                }
            }
            """;

        var complexityConfiguration = new ComplexityConfiguration();
        var res = Execute(complexityConfiguration, query);

        res.Result.Errors.ShouldNotBe(null);
        res.Result.Errors.Count.ShouldBe(3);
        res.Result.Errors.All(e => e is not ComplexityError);
    }

    // https://github.com/graphql-dotnet/graphql-dotnet/issues/3527
    [Fact]
    public void recursive_fragment_should_not_end_in_eternal_loop2()
    {
        const string query = """
            {
                type_All(limit: 20) {
                    items {
                        links(limit: 20) {
                            items {
                                ... RecursiveFragment
                            }
                        }
                    }
                }
            }
            fragment RecursiveFragment on Type   {
               links(limit: 20) {
                    items {
                        ... RecursiveFragment
                    }
                }
            }
            """;

        var complexityConfiguration = new ComplexityConfiguration();
        var res = Execute(complexityConfiguration, query, onlyComplexityRule: true);

        res.Result.Errors.ShouldNotBe(null);
        res.Result.Errors.Count.ShouldBe(1);
        res.Result.Errors[0].ShouldBeOfType<ValidationError>().Message.ShouldBe("It looks like document has fragment cycle. Please make sure you are using standard validation rules especially NoFragmentCycles one.");
    }
}
