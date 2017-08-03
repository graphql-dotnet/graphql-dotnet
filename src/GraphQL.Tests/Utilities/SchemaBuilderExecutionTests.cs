using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Utilities
{
    public class SchemaBuilderExecutionTests : SchemaBuilderTestBase
    {
        [Fact]
        public void can_execute_resolver()
        {
            var defs = @"
                type Post {
                    id: ID!
                    title: String!
                }

                type Query {
                    post(id: ID!): Post
                }
            ";

            Builder.Types.Include<PostQueryType>();

            var query = @"query Posts($id: ID!) { post(id: $id) { id title } }";
            var expected = @"{ 'post': { 'id' : '1', 'title': 'Post One' } }";
            var variables = "{ 'id': '1' }";

            AssertQuery(_ =>
            {
                _.Query = query;
                _.Definitions = defs;
                _.Variables = variables;
                _.ExpectedResult = expected;
            });
        }

        [Fact]
        public void can_provide_field_description()
        {
            var defs = @"
                type Post {
                    id: ID!
                    title: String!
                }

                type Query {
                    post(id: ID!): Post
                }
            ";

            Builder.Types.Include<PostQueryRenamedType>();
            var schema = Builder.Build(defs);

            var field = schema.Query.Fields.Single();
            field.Description.ShouldBe("A description");
        }

        [Fact]
        public void can_execute_renamed_field()
        {
            var defs = @"
                type Post {
                    id: ID!
                    title: String!
                }

                type Query {
                    post(id: ID!): Post
                }
            ";

            Builder.Types.Include<PostQueryRenamedType>();

            var query = @"query Posts($id: ID!) { post(id: $id) { id title } }";
            var expected = @"{ 'post': { 'id' : '1', 'title': 'Post One' } }";
            var variables = "{ 'id': '1' }";

            AssertQuery(_ =>
            {
                _.Query = query;
                _.Definitions = defs;
                _.Variables = variables;
                _.ExpectedResult = expected;
            });
        }

        [Fact]
        public void can_execute_interfaces()
        {
            var defs = @"
                enum PetKind {
                    CAT
                    DOG
                }

                interface Pet {
                    name: String!
                }

                type Dog implements Pet {
                    name: String!
                    barks: Boolean!
                }

                type Cat implements Pet {
                    name: String!
                    meows: Boolean!
                }

                type Query {
                    pet(type: PetKind = DOG): Pet
                }
            ";

            Builder.Types.For("Dog").IsTypeOf<Dog>();
            Builder.Types.For("Cat").IsTypeOf<Cat>();
            Builder.Types.Include<PetQueryType>();

            var query = @"{ pet { name } }";
            var expected = @"{ 'pet': { 'name' : 'Eli' } }";

            AssertQuery(_ =>
            {
                _.Query = query;
                _.Definitions = defs;
                _.ExpectedResult = expected;
            });
        }

        [Fact]
        public void minimal_schema()
        {
            var schema = Schema.For(@"
                type Query {
                  hello: String
                }
            ");

            var root = new { Hello = "Hello World!" };
            var result = schema.Execute(_ =>
            {
                _.Query = "{ hello }";
                _.Root = root;
            });

            var expectedResult = CreateQueryResult("{ 'hello': 'Hello World!' }");
            var serializedExpectedResult = Writer.Write(expectedResult);

            result.ShouldBe(serializedExpectedResult);
        }
    }

    public class PostData
    {
        public static List<Post> Posts = new List<Post>
        {
            new Post {Id = "1", Title = "Post One"}
        };
    }

    public class Post
    {
        public string Id { get; set; }
        public string Title { get; set; }
    }

    [GraphQLMetadata("Query")]
    public class PostQueryType
    {
        public Post Post(string id)
        {
            return PostData.Posts.FirstOrDefault(x => x.Id == id);
        }
    }

    [GraphQLMetadata("Query")]
    public class PostQueryRenamedType
    {
        [GraphQLMetadata("post", Description = "A description")]
        public Post GetPostById(string id)
        {
            return PostData.Posts.FirstOrDefault(x => x.Id == id);
        }
    }

    abstract class Pet
    {
        public string Name { get; set; }
    }

    class Dog : Pet
    {
        public bool Barks { get; set; }
    }

    class Cat : Pet
    {
        public bool Meows { get; set; }
    }

    enum PetKind
    {
        Cat,
        Dog
    }

    [GraphQLMetadata("Query")]
    class PetQueryType
    {
        public Pet Pet(PetKind type)
        {
            if (type == PetKind.Dog)
            {
                return new Dog {Name = "Eli", Barks = true};
            }

            return new Cat {Name = "Biscuit", Meows = true};
        }
    }
}
