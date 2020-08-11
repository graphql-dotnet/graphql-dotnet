using GraphQL.Execution;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using Shouldly;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    // https://github.com/graphql-dotnet/graphql-dotnet/pulls/1770
    public class Bug1770 : QueryTestBase<Bug1770Schema>
    {
        [Theory]
        [InlineData("")]
        [InlineData((string)null)]
        [InlineData("firstQuery")]
        [InlineData("secondQuery")]
        public async Task DocumentExecuter_works_for_valid_operation(string operationName)
        {
            var de = new DocumentExecuter();
            var valid = await de.ExecuteAsync(new ExecutionOptions
            {
                Query = "query firstQuery {test} query secondQuery {test}",
                Schema = Schema,
                OperationName = operationName,
            });
            valid.ShouldNotBeNull();
            valid.Data.ShouldNotBeNull();
            valid.Errors.ShouldBeNull();
        }

        [Theory]
        [InlineData("thirdQuery")]
        [InlineData("query")]
        [InlineData("test")]
        public async Task DocumentExecuter_throws_for_invalid_operation(string operationName)
        {
            var de = new DocumentExecuter();
            var result = await de.ExecuteAsync(new ExecutionOptions()
                {
                    Query = "query firstQuery {test} query secondQuery {test}",
                    Schema = Schema,
                    OperationName = operationName
                });
            result.ShouldNotBeNull();
            result.Data.ShouldBeNull();
            result.Errors.ShouldNotBeNull();
            result.Errors.Count.ShouldBe(1);
            result.Errors[0].Message.ShouldBe($"Query does not contain operation '{operationName}'.");
            result.Errors[0].InnerException.ShouldNotBeNull();
            result.Errors[0].InnerException.ShouldBeOfType<InvalidOperationException>();
        }
    }

    public class Bug1770Schema : Schema
    {
        public Bug1770Schema()
        {
            Query = new Bug1770Query();
        }
    }

    public class Bug1770Query : ObjectGraphType
    {
        public Bug1770Query()
        {
            Field<StringGraphType>("Test", resolve: context => "ok");
        }
    }
}
