using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        [Fact]
        public void can_use_source_without_params()
        {
            var schema = Schema.For(@"
                type Query {
                  source: Boolean
                }
            ", _=>
            {
                _.Types.Include<ParametersType>();
            });

            var result = schema.Execute(_ =>
            {
                _.Query = "{ source }";
                _.Root = new { Hello =  "World" };
            });

            var expectedResult = CreateQueryResult("{ 'source': true }");
            var serializedExpectedResult = Writer.Write(expectedResult);

            result.ShouldBe(serializedExpectedResult);
        }

        [Fact]
        public void can_use_resolvefieldcontext_without_params()
        {
            var schema = Schema.For(@"
                type Query {
                  resolve: String
                }
            ", _=>
            {
                _.Types.Include<ParametersType>();
            });

            var result = schema.Execute(_ =>
            {
                _.Query = "{ resolve }";
                _.ExposeExceptions = true;
            });

            var expectedResult = CreateQueryResult("{ 'resolve': 'Resolved' }");
            var serializedExpectedResult = Writer.Write(expectedResult);

            result.ShouldBe(serializedExpectedResult);
        }

        [Fact]
        public void can_use_resolvefieldcontext_with_params()
        {
            var schema = Schema.For(@"
                type Query {
                  resolveWithParam(id: String): String
                }
            ", _=>
            {
                _.Types.Include<ParametersType>();
            });

            var result = schema.Execute(_ =>
            {
                _.Query = @"{ resolveWithParam(id: ""abcd"") }";
            });

            var expectedResult = CreateQueryResult("{ 'resolveWithParam': 'Resolved abcd' }");
            var serializedExpectedResult = Writer.Write(expectedResult);

            result.ShouldBe(serializedExpectedResult);
        }

        [Fact]
        public void can_use_usercontext()
        {
            var schema = Schema.For(@"
                type Query {
                  userContext: String
                }
            ", _=>
            {
                _.Types.Include<ParametersType>();
            });

            var result = schema.Execute(_ =>
            {
                _.Query = @"{ userContext }";
                _.UserContext = new MyUserContext { Name = "Quinn" };
            });

            var expectedResult = CreateQueryResult("{ 'userContext': 'Quinn' }");
            var serializedExpectedResult = Writer.Write(expectedResult);

            result.ShouldBe(serializedExpectedResult);
        }

        [Fact]
        public void can_use_usercontext_with_params()
        {
            var schema = Schema.For(@"
                type Query {
                  userContextWithParam(id: String): String
                }
            ", _=>
            {
                _.Types.Include<ParametersType>();
            });

            var result = schema.Execute(_ =>
            {
                _.Query = @"{ userContextWithParam(id: ""abcd"") }";
                _.UserContext = new MyUserContext { Name = "Quinn" };
            });

            var expectedResult = CreateQueryResult("{ 'userContextWithParam': 'Quinn abcd' }");
            var serializedExpectedResult = Writer.Write(expectedResult);

            result.ShouldBe(serializedExpectedResult);
        }

        [Fact]
        public void can_use_context_source_usercontext()
        {
            var schema = Schema.For(@"
                type Query {
                  three: Boolean
                }
            ", _=>
            {
                _.Types.Include<ParametersType>();
            });

            var result = schema.Execute(_ =>
            {
                _.Query = @"{ three }";
                _.Root = new { Hello = "World" };
                _.UserContext = new MyUserContext { Name = "Quinn" };
            });

            var expectedResult = CreateQueryResult("{ 'three': true }");
            var serializedExpectedResult = Writer.Write(expectedResult);

            result.ShouldBe(serializedExpectedResult);
        }

        [Fact]
        public void can_use_context_source_usercontext_with_params()
        {
            var schema = Schema.For(@"
                type Query {
                  four(id: Int): Boolean
                }
            ", _=>
            {
                _.Types.Include<ParametersType>();
            });

            var result = schema.Execute(_ =>
            {
                _.Query = @"{ four(id: 123) }";
                _.Root = new { Hello = "World" };
                _.UserContext = new MyUserContext { Name = "Quinn" };
            });

            var expectedResult = CreateQueryResult("{ 'four': true }");
            var serializedExpectedResult = Writer.Write(expectedResult);

            result.ShouldBe(serializedExpectedResult);
        }

        [Fact]
        public void can_execute_complex_schema()
        {
            var defs = @"
                type Post {
                    id: ID!
                    title: String!
                }
                type Blog {
                    title: String!
                    post(id: ID!): Post
                }
                type Query {
                    blog(id: ID!): Blog
                }
            ";

            Builder.Types.Include<BlogQueryType>();
            Builder.Types.Include<Blog>();

            var query = @"query Posts($blogId: ID!, $postId: ID!){ blog(id: $blogId){ title post(id: $postId) { id title } } }";
            var expected = @"{ 'blog': { 'title': 'New blog', 'post': { 'id' : '1', 'title': 'Post One' } } }";
            var variables = "{ 'blogId': '1', 'postId': '1' }";

            AssertQuery(_ =>
            {
                _.Query = query;
                _.Definitions = defs;
                _.ExpectedResult = expected;
                _.Variables = variables;
            });
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

    [GraphQLMetadata("Blog")]
    public class Blog
    {
        public string Title { get; set; }
        public Post Post(string id)
        {
            return PostData.Posts.FirstOrDefault(x => x.Id == id);
        }
    }

    [GraphQLMetadata("Query")]
    public class BlogQueryType
    {
        public Blog Blog(string id)
        {
            return new Blog
            {
                Title = "New blog"
            };
        }
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

    [GraphQLMetadata("Query")]
    class ParametersType
    {
        public bool Source(object source)
        {
            return source != null;
        }

        public string Resolve(ResolveFieldContext context)
        {
            return "Resolved";
        }

        public string ResolveWithParam(ResolveFieldContext context, string id)
        {
            return $"Resolved {id}";
        }

        public string UserContext(MyUserContext context)
        {
            return context.Name;
        }

        public string UserContextWithParam(MyUserContext context, string id)
        {
            return $"{context.Name} {id}";
        }

        public bool Three(ResolveFieldContext resolveContext, object source, MyUserContext context)
        {
            return resolveContext != null && context != null && source != null;
        }

        public bool Four(ResolveFieldContext resolveContext, object source, MyUserContext context, int id)
        {
            return resolveContext != null && context != null && source != null && id != 0;
        }
    }

    class MyUserContext
    {
        public string Name { get; set; }
    }
}
