using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Execution
{
    public class Issue2275
    {
        [Theory]
        [ClassData(typeof(DocumentWritersTestData))]
        public async Task should_map(IDocumentWriter documentWriter)
        {
            var documentExecuter = new DocumentExecuter();
            var executionResult = await documentExecuter.ExecuteAsync(_ =>
            {
                _.Schema = new Issue2275Schema();
                _.Query = @"query($data:Input!) {
                                request(data: $data)
                }";
                _.Inputs = @" {
                    ""data"": {
                        ""clientId"": 2,
                        ""filters"": [{
                            ""key"": ""o"",
                            ""value"": 25
                        }]
                    }
                }".ToInputs();
            });

            var json = await documentWriter.WriteToStringAsync(executionResult);
            executionResult.Errors.ShouldBeNull();

            json.ShouldBe(@"{
  ""data"": {
    ""request"": ""2: [o=25]""
  }
}");
        }

        private class Issue2275Schema : Schema
        {
            public Issue2275Schema()
            {
                Query = new Issue2275Query();
            }
        }

        private class Issue2275Query : ObjectGraphType<object>
        {
            public Issue2275Query()
            {
                Field<StringGraphType>(
                  "request",
                  arguments: new QueryArguments(
                      new QueryArgument<NonNullGraphType<Issue2275InputType>> { Name = "data", Description = "some stuff" }
                  ),
                  resolve: context => context.GetArgument<ContainerRequest>("data").ToString()
              );
            }
        }

        public class FilterInputGraphType : InputObjectGraphType<Filter>
        {
            public FilterInputGraphType()
            {
                Name = "FilterInput";
                Field(x => x.Key);
                Field(x => x.Value);
            }
        }

        [GraphQLMetadata(InputType = typeof(FilterInputGraphType))]
        public class Filter
        {
            public string Key { get; set; }
            public int Value { get; set; }
        }

        public class KeywordsTemplate
        {
            public string Query { get; set; }
            public string UserQuery { get; set; }
        }

        public class ContainerRequest
        {
            public IList<Filter> Filters { get; set; }
            public KeywordsTemplate KeywordsTemplate { get; set; }
            public int ClientId { get; set; }
            public int LanguageId { get; set; }
            public int CustomerId { get; set; }
            public int AppId { get; set; }

            public override string ToString() => $"{ClientId}: [{string.Join("|", Filters.Select(f => f.Key + "=" + f.Value))}]";
        }

        public class Issue2275InputType : InputObjectGraphType<ContainerRequest>
        {
            public Issue2275InputType()
            {
                Name = "Input";
                Field(x => x.ClientId);
                Field(x => x.Filters);
                Field(x => x.AppId, nullable: true);
                Field(x => x.CustomerId, nullable: true);
                Field(x => x.LanguageId, nullable: true);
            }
        }
    }
}
