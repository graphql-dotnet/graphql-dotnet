using System;
using Xunit;
using GraphQL.Tests.StarWars;
using GraphQL.Federation.Instrumentation;
using GraphQL.Instrumentation;
using Mdg.Engine.Proto;
using Shouldly;

namespace GraphQL.Federation.Tests
{
    public class ApolloFederatedTracingTests : StarWarsTestBase
    {
        [Fact]
        public void extension_has_expected_format()
        {
            var query = @"
query {
  hero {
    name
    friends {
      name
    }
  }
}";

            var start = DateTime.UtcNow;
            Schema.FieldMiddleware.Use(new FederatedInstrumentFieldMiddleware());
            var result = Executer.ExecuteAsync(_ =>
            {
                _.Schema = Schema;
                _.Query = query;
                _.EnableMetrics = true;
            }).Result;

            result.EnrichWithApolloFederatedTracing(start);
            string federaredTrace = (string)result.Extensions["ftv1"];
            byte[] bytes = Convert.FromBase64String(federaredTrace);
            var trace = Trace.Parser.ParseFrom(bytes);

            trace.StartTime.ToDateTime().ShouldBeGreaterThan(DateTime.Now);
            trace.EndTime.ToDateTime().ShouldBeGreaterThan(trace.StartTime.ToDateTime());
            ((long)trace.DurationNs).ShouldBeGreaterThan(0);
            trace.Root.ShouldNotBeNull();
            trace.Root.Child.Count.ShouldBe(1);

        }

        /*[Fact]
        public async Task serialization_should_have_correct_case()
        {
            var trace = new ApolloTrace(new DateTime(2019, 12, 05, 15, 38, 00, DateTimeKind.Utc), 102.5);
            var expected = @"{
  ""version"": 1,
  ""startTime"": ""2019-12-05T15:38:00Z"",
  ""endTime"": ""2019-12-05T15:38:00.103Z"",
  ""duration"": 102500000,
  ""parsing"": {
    ""startOffset"": 0,
    ""duration"": 0
  },
  ""validation"": {
    ""startOffset"": 0,
    ""duration"": 0
  },
  ""execution"": {
    ""resolvers"": []
  }
}";

            var result = await Writer.WriteToStringAsync(trace);

            result.ShouldBeCrossPlat(expected);
        }*/
    }
}
