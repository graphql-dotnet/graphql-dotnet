using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace GraphQL.Tests.Utilities
{
    public class TypedExecuteConfig
    {
        public IEnumerable<Type> Types { get; set; }
        public string Query { get; set; }
        public string Variables { get; set; }
        public string ExpectedResult { get; set; }
    }

    public class TypedSchemaBuilderTests : TypedSchemaBuilderTestBase
    {
        public class Post
        {
            public Guid Id { get; set; }
            public string Body { get; set; }
        }

        public class Query
        {
            [GraphQLMetadata("post")]
            public Post GetPost(Guid id)
            {
                return new Post {Id = id, Body = "Something"};
            }

            [GraphQLMetadata("postAsync")]
            public Task<Post> GetPostAsync(Guid id)
            {
                return Task.FromResult(new Post {Id = id, Body = "Something"});
            }

            [GraphQLMetadata("postList")]
            public IEnumerable<Post> GetPostList()
            {
                return new[] {new Post {Id = Guid.NewGuid(), Body = "Something"}};
            }
        }

        [Fact]
        public void can_run_basic_query()
        {
            var id = Guid.NewGuid(); 

            AssertQuery(_ =>
            {
                _.Query = $"{{ post(id: \"{id}\") {{ id body }} }}";
                _.Types = new[] {typeof(Query)};
                _.ExpectedResult = "{'post': { 'id': '" + id + "', 'body': 'Something' }}";
            });
        }

        [Fact]
        public void can_run_async_query()
        {
            var id = Guid.NewGuid(); 

            AssertQuery(_ =>
            {
                _.Query = $"{{ postAsync(id: \"{id}\") {{ id body }} }}";
                _.Types = new[] {typeof(Query)};
                _.ExpectedResult = "{'postAsync': { 'id': '" + id + "', 'body': 'Something' }}";
            });
        }

        [Fact]
        public void can_handle_enumerable_return_type()
        {
            AssertQuery(_ =>
            {
                _.Query = $"{{ postList {{ body }} }}";
                _.Types = new[] {typeof(Query)};
                _.ExpectedResult = "{ 'postList': [{ 'body': 'Something' }] }";
            });
        }
    }
}
