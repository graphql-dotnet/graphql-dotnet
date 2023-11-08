using GraphQL.Instrumentation;
using GraphQL.StarWars;
using GraphQL.Tests.StarWars;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Instrumentation;

public class ApolloTracingTests : StarWarsTestBase
{
    [Fact]
    public async Task extension_has_expected_format()
    {
        const string query = """
            query {
              hero {
                name
                friends {
                  name
                  aliasedName: name
                }
              }
            }
            """;

        var start = DateTime.UtcNow;
        Schema.FieldMiddleware.Use(new InstrumentFieldsMiddleware());
        var result = await Executer.ExecuteAsync(_ =>
        {
            _.Schema = Schema;
            _.Query = query;
            _.EnableMetrics = true;
        });
        result.EnrichWithApolloTracing(start);
        var trace = (ApolloTrace)result.Extensions["tracing"];

        trace.Version.ShouldBe(1);
        trace.Parsing.StartOffset.ShouldNotBe(0);
        trace.Parsing.Duration.ShouldNotBe(0);
        trace.Validation.StartOffset.ShouldNotBe(0);
        trace.Validation.Duration.ShouldNotBe(0);
        trace.Validation.StartOffset.ShouldNotBeSameAs(trace.Parsing.StartOffset);
        trace.Validation.Duration.ShouldNotBeSameAs(trace.Parsing.Duration);

        var expectedPaths = new List<(List<object> Path, string ParentType, string FieldName, string ReturnType)>
        {
            (new List<object> { "hero" }, "Query", "hero", "Character"),
            (new List<object> { "hero", "name" }, "Droid", "name", "String"),
            (new List<object> { "hero", "friends" }, "Droid", "friends", "[Character]"),
            (new List<object> { "hero", "friends", 0, "name" }, "Human", "name", "String"),
            (new List<object> { "hero", "friends", 0, "aliasedName" }, "Human", "name", "String"),
            (new List<object> { "hero", "friends", 1, "name" }, "Droid", "name", "String"),
            (new List<object> { "hero", "friends", 1, "aliasedName" }, "Droid" , "name" , "String"),
        };

        trace.Execution.Resolvers.Count.ShouldBe(expectedPaths.Count);

        var index = 0;
        foreach (var resolver in trace.Execution.Resolvers)
        {
            resolver.StartOffset.ShouldNotBe(0);
            resolver.Duration.ShouldNotBe(0);
            var expected = expectedPaths[index++];
            resolver.ParentType.ShouldBe(expected.ParentType);
            resolver.FieldName.ShouldBe(expected.FieldName);
            resolver.ReturnType.ShouldBe(expected.ReturnType);
            resolver.Path.ShouldBe(expected.Path);
        }
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void serialization_should_have_correct_case(IGraphQLTextSerializer writer)
    {
        var trace = new ApolloTrace(new DateTime(2019, 12, 05, 15, 38, 00, DateTimeKind.Utc), 102.5);
        const string expected = """
        {
          "version": 1,
          "startTime": "2019-12-05T15:38:00Z",
          "endTime": "2019-12-05T15:38:00.1025Z",
          "duration": 102500000,
          "parsing": {
            "startOffset": 0,
            "duration": 0
          },
          "validation": {
            "startOffset": 0,
            "duration": 0
          },
          "execution": {
            "resolvers": []
          }
        }
        """;

        string result = writer.Serialize(trace);

        result.ShouldBeCrossPlat(expected);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public async Task AddApolloTracing_Works(bool enable, bool enableBefore, bool enableAfter)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<StarWarsData>();
        serviceCollection.AddGraphQL(b => b
            .AddSelfActivatingSchema<StarWarsSchema>()
            .ConfigureExecution((opts, next) =>
            {
                opts.EnableMetrics.ShouldBeFalse();
                if (enableBefore)
                    opts.EnableMetrics = true;
                return next(opts);
            })
            .UseApolloTracing(enable)
            .ConfigureExecution((opts, next) =>
            {
                opts.EnableMetrics.ShouldBe(enable || enableBefore);
                if (enableAfter)
                    opts.EnableMetrics = true;
                return next(opts);
            })
            .AddSystemTextJson());
        using var provider = serviceCollection.BuildServiceProvider();
        var executer = provider.GetRequiredService<IDocumentExecuter<ISchema>>();
        var serializer = provider.GetRequiredService<IGraphQLTextSerializer>();
        var result = await executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "{ hero { name } }",
            RequestServices = provider,
        });
        string resultString = serializer.Serialize(result);
        if (enable || enableAfter || enableBefore)
        {
            resultString.ShouldStartWith("""{"data":{"hero":{"name":"R2-D2"}},"extensions":{"tracing":{"version":1,"startTime":"2""");
        }
        else
        {
            resultString.ShouldBe("""{"data":{"hero":{"name":"R2-D2"}}}""");
        }
    }
}
