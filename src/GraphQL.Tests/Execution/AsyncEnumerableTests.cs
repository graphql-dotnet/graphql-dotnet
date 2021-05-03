using System;
using System.Collections.Generic;
using GraphQL.Resolvers;
using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Execution
{
    public class AsyncEnumerableTests : QueryTestBase<AsyncEnumerableTests.AsyncEnumerableSchema>
    {
        [Fact]
        public void Should_Resolve_IAsyncEnumerable_Of_Int()
        {
            AssertQuerySuccess(
                @"query {
                    intField
                }",
                @"{
                    ""intField"": [1, 2, 3]
                }");
        }

        public class AsyncEnumerableSchema : Schema
        {
            public AsyncEnumerableSchema(AsyncEnumerableQueryType query)
            {
                Query = query;
            }
        }

        public class AsyncEnumerableQueryType : ObjectGraphType
        {
            public AsyncEnumerableQueryType()
            {
                Field<ListGraphType<IntGraphType>, IEnumerable<int>>()
                    .Name("intField")
                    .Configure(field => field.Resolver = new FuncFieldResolver<object>(_ => ToAsyncEnumerable(new[] { 1, 2, 3 })));
             }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> items)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                foreach (var item in items)
                {
                    yield return item;
                }
            }
        }
    }
}
