using GraphQL.Execution;
using GraphQL.Types;

namespace GraphQL.Tests.Utilities;

public class SchemaBuilderExecutionTests : SchemaBuilderTestBase
{
    public class Query
    {
        public virtual Test Test() => new Test();

        public string Method() => throw new OverflowException("just test");

        public int Property => throw new DivideByZeroException("test too");
    }

    public class Test
    {
        public string Id => "foo";
    }

    public class TestEx : Test
    {
        public string Name => "bar";
    }

    public class QueryEx : Query
    {
        public override Test Test() => new TestEx();
    }

    [Fact]
    public async Task schema_first_generate_exception_with_normal_stack_trace_for_method()
    {
        var schema = Schema.For(@"
                type Query {
                    method: String!
                    property: Int!
                }
                ", builder => builder.Types.Include<Query>());

        var executor = new DocumentExecuter();
        var result = await executor.ExecuteAsync(options =>
        {
            options.Schema = schema;
            options.Query = "{ method }";
        }).ConfigureAwait(false);

        result.Errors.Count.ShouldBe(1);
        result.Errors[0].Code.ShouldBe("OVERFLOW");
        result.Errors[0].Message.ShouldBe("Error trying to resolve field 'method'.");

        var stack = result.Errors[0].InnerException.ShouldBeOfType<OverflowException>().StackTrace;
        if (stack.StartsWith("   в "))
            stack = stack.Remove(0, 5);
        if (stack.StartsWith("   at "))
            stack = stack.Remove(0, 6);
        if (stack.StartsWith("   在 "))
            stack = stack.Remove(0, 5);

        stack.ShouldStartWith("GraphQL.Tests.Utilities.SchemaBuilderExecutionTests.Query.Method()");
    }

    [Fact]
    public async Task schema_first_generate_exception_with_normal_stack_trace_for_property()
    {
        var schema = Schema.For(@"
                type Query {
                    method: String!
                    property: Int!
                }
                ", builder => builder.Types.Include<Query>());

        var executor = new DocumentExecuter();
        var result = await executor.ExecuteAsync(options =>
        {
            options.Schema = schema;
            options.Query = "{ property }";
        }).ConfigureAwait(false);

        result.Errors.Count.ShouldBe(1);
        result.Errors[0].Code.ShouldBe("DIVIDE_BY_ZERO");
        result.Errors[0].Message.ShouldBe("Error trying to resolve field 'property'.");

        var stack = result.Errors[0].InnerException.ShouldBeOfType<DivideByZeroException>().StackTrace;
        if (stack.StartsWith("   в "))
            stack = stack.Remove(0, 5);
        if (stack.StartsWith("   at "))
            stack = stack.Remove(0, 6);
        if (stack.StartsWith("   在 "))
            stack = stack.Remove(0, 5);

        stack.ShouldStartWith("GraphQL.Tests.Utilities.SchemaBuilderExecutionTests.Query.get_Property()");
    }

    [Fact]
    public async Task issue_1155_throws()
    {
        var schema = Schema.For(@"
                type Test {
                    id: ID!
                    name: String!
                }

                type Query {
                    test: Test!
                }
                ", builder => builder.Types.Include<Query>());

        var executor = new DocumentExecuter();
        var result = await executor.ExecuteAsync(options =>
        {
            options.Schema = schema;
            options.Query = "{ test { id name } }";
        }).ConfigureAwait(false);

        result.Errors.Count.ShouldBe(1);
        result.Errors[0].Code.ShouldBe("INVALID_OPERATION");
        result.Errors[0].Message.ShouldBe("Error trying to resolve field 'name'.");
        result.Errors[0].InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Expected to find property or method 'name' on type 'Test' but it does not exist.");
    }

    [Fact]
    public async Task issue_1155_with_custom_root_does_not_throw()
    {
        var schema = Schema.For(@"
                type Test {
                    id: ID!
                    name: String!
                }

                type Query {
                    test: Test!
                }
                ", builder => builder.Types.Include<Query>());

        var executor = new DocumentExecuter();
        var result = await executor.ExecuteAsync(options =>
        {
            options.Schema = schema;
            options.Query = "{ test { id name } }";
            options.Root = new QueryEx();
        }).ConfigureAwait(false);

        result.Errors.ShouldBeNull();
        var data = result.Data.ShouldBeAssignableTo<RootExecutionNode>();
        var t = data.ToDict()["test"].ShouldBeAssignableTo<IReadOnlyDictionary<string, object>>().ToDict();
        t["id"].ShouldBe("foo");
        t["name"].ShouldBe("bar");
    }

    [Fact]
    public void issue_1155_throws_on_build_if_AllowUnknownTypes_disabled()
    {
        Should.Throw<InvalidOperationException>(() => Schema.For(@"
                type Test {
                    id: ID!
                    name: String!
                }

                type Query {
                    test: Test!
                }
                ", builder =>
        {
            builder.AllowUnknownTypes = false;
            builder.Types.Include<Query>();
        })).Message.ShouldBe("Unknown type 'Test'. Verify that you have configured SchemaBuilder correctly.");
    }

    [Fact]
    public void issue_1155_throws_on_build_if_AllowUnknownFields_disabled_1()
    {
        Should.Throw<InvalidOperationException>(() => Schema.For(@"
                type Test {
                    id: ID!
                    name: String!
                }

                type Query {
                    test: Test!
                }
                ", builder =>
               {
                   builder.AllowUnknownFields = false;
                   builder.Types.Include<Query>();
               })).Message.ShouldBe("Unknown field 'Test.id' has no resolver. Verify that you have configured SchemaBuilder correctly.");
    }

    [Fact]
    public void issue_1155_throws_on_build_if_AllowUnknownFields_disabled_2()
    {
        Should.Throw<InvalidOperationException>(() => Schema.For(@"
                type Test {
                    id: ID!
                    name: String!
                }

                type Query {
                    test: Test!
                }
                ", builder =>
        {
            builder.AllowUnknownFields = false;
            builder.Types.Include<Query>();
            builder.Types.Include<Test>();
        })).Message.ShouldBe("Unknown field 'Test.name' has no resolver. Verify that you have configured SchemaBuilder correctly.");
    }

    [Fact]
    public void can_read_schema_with_custom_root_names()
    {
        var schema = Schema.For("CustomSubscription".ReadSDL());

        schema.Query.Name.ShouldBe("CustomQuery");
        schema.Mutation.Name.ShouldBe("CustomMutation");
        schema.Subscription.Name.ShouldBe("CustomSubscription");
    }

    [Theory]
    [InlineData("PetAfterAll", 15)]
    [InlineData("PetBeforeAll", 15)]
    public void can_read_schema(string fileName, int expectedCount)
    {
        var schema = Schema.For(
            fileName.ReadSDL(),
            builder => builder.Types.ForAll(config => config.ResolveType = _ => null)
        );

        schema.AllTypes.Count.ShouldBe(expectedCount);
    }

    [Fact]
    public void can_read_complex_schema()
    {
        var schema = Schema.For(
            "PetComplex".ReadSDL(),
            builder =>
            {
                builder.Types.ForAll(config => config.ResolveType = _ => null);
                builder.IgnoreComments = false;
            }
        );

        schema.Description.ShouldBe("Animals - cats and dogs");
        schema.AllTypes.Count.ShouldBe(16);

        var cat = schema.AllTypes.OfType<IComplexGraphType>().First(t => t.Name == "Cat");
        cat.Description.ShouldBe(" A cat");
        cat.GetField("name").Description.ShouldBe(" cat's name");
        cat.GetField("weight").Arguments[0].Name.ShouldBe("inPounds");
        cat.GetField("weight").Arguments[0].ResolvedType.GetType().ShouldBe(typeof(BooleanGraphType));
        cat.GetField("weight").Arguments[0].Description.ShouldBe("comment on argument");
        var dog = schema.AllTypes.OfType<IComplexGraphType>().First(t => t.Name == "Dog");
        dog.Description.ShouldBe(" A dog");
        dog.GetField("age").Description.ShouldBe(" dog's age");

        var pet = schema.AllTypes.OfType<UnionGraphType>().First(t => t.Name == "Pet");
        pet.Description.ShouldBe("Cats with dogs");
        pet.PossibleTypes.Count.ShouldBe(2);

        var query = schema.AllTypes.OfType<IComplexGraphType>().First(t => t.Name == "Query");
        query.GetField("allAnimalsCount").DeprecationReason.ShouldBe("do not touch!");
        query.GetField("catsGroups").ResolvedType.ToString().ShouldBe("[[Cat!]!]!");
    }

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
        var expected = @"{ ""post"": { ""id"" : ""1"", ""title"": ""Post One"" } }";
        var variables = @"{ ""id"": ""1"" }";

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
        var expected = @"{ ""post"": { ""id"" : ""1"", ""title"": ""Post One"" } }";
        var variables = @"{ ""id"": ""1"" }";

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
        var expected = @"{ ""pet"": { ""name"" : ""Eli"" } }";

        AssertQuery(_ =>
        {
            _.Query = query;
            _.Definitions = defs;
            _.ExpectedResult = expected;
        });
    }

    // https://github.com/graphql-dotnet/graphql-dotnet/issues/1795
    [Fact]
    public void schemabuilder_should_throw_on_invalid_default_value()
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
                    pet(type: PetKind = DOGGY): Pet
                }
            ";

        Builder.Types.For("Dog").IsTypeOf<Dog>();
        Builder.Types.For("Cat").IsTypeOf<Cat>();
        Builder.Types.Include<PetQueryType>();

        Should.Throw<InvalidOperationException>(() => Builder.Build(defs).Initialize());
    }

    [Fact]
    public async Task minimal_schema()
    {
        var schema = Schema.For(@"
                type Query {
                  hello: String
                }
            ");

        var root = new { Hello = "Hello World!" };
        var result = await ExecuteAsync(schema, _ =>
        {
            _.Query = "{ hello }";
            _.Root = root;
        }).ConfigureAwait(false);

        var expectedResult = CreateQueryResult(@"{ ""hello"": ""Hello World!"" }");
        var serializedExpectedResult = Serializer.Serialize(expectedResult);

        result.ShouldBe(serializedExpectedResult);
    }

    [Fact]
    public async Task can_use_source_without_params()
    {
        var schema = Schema.For(@"
                type Query {
                  source: Boolean
                }
            ", _ => _.Types.Include<ParametersType>());

        var result = await ExecuteAsync(schema, _ =>
        {
            _.Query = "{ source }";
            _.Root = new { Hello = "World" };
        }).ConfigureAwait(false);

        var expectedResult = CreateQueryResult(@"{ ""source"": true }");
        var serializedExpectedResult = Serializer.Serialize(expectedResult);

        result.ShouldBe(serializedExpectedResult);
    }

    [Fact]
    public async Task can_use_resolvefieldcontext_without_params()
    {
        var schema = Schema.For(@"
                type Query {
                  resolve: String
                }
            ", _ => _.Types.Include<ParametersType>());

        var result = await ExecuteAsync(schema, _ => _.Query = "{ resolve }").ConfigureAwait(false);

        var expectedResult = CreateQueryResult(@"{ ""resolve"": ""Resolved"" }");
        var serializedExpectedResult = Serializer.Serialize(expectedResult);

        result.ShouldBe(serializedExpectedResult);
    }

    [Fact]
    public async Task can_use_resolvefieldcontext_with_params()
    {
        var schema = Schema.For(@"
                type Query {
                  resolveWithParam(id: String): String
                }
            ", _ => _.Types.Include<ParametersType>());

        var result = await ExecuteAsync(schema, _ => _.Query = @"{ resolveWithParam(id: ""abcd"") }").ConfigureAwait(false);

        var expectedResult = CreateQueryResult(@"{ ""resolveWithParam"": ""Resolved abcd"" }");
        var serializedExpectedResult = Serializer.Serialize(expectedResult);

        result.ShouldBe(serializedExpectedResult);
    }

    [Fact]
    public async Task can_use_usercontext()
    {
        var schema = Schema.For(@"
                type Query {
                  userContext: String
                }
            ", _ => _.Types.Include<ParametersType>());

        var result = await ExecuteAsync(schema, _ =>
        {
            _.Query = @"{ userContext }";
            _.UserContext = new MyUserContext { Name = "Quinn" };
        }).ConfigureAwait(false);

        var expectedResult = CreateQueryResult(@"{ ""userContext"": ""Quinn"" }");
        var serializedExpectedResult = Serializer.Serialize(expectedResult);

        result.ShouldBe(serializedExpectedResult);
    }

    [Fact]
    public async Task can_use_inherited_usercontext()
    {
        var schema = Schema.For(@"
                type Query {
                  userContext: String
                }
            ", _ => _.Types.Include<ParametersType>());

        var result = await ExecuteAsync(schema, _ =>
        {
            _.Query = @"{ userContext }";
            _.UserContext = new ChildMyUserContext { Name = "Quinn" };
        }).ConfigureAwait(false);

        var expectedResult = CreateQueryResult(@"{ ""userContext"": ""Quinn"" }");
        var serializedExpectedResult = Serializer.Serialize(expectedResult);

        result.ShouldBe(serializedExpectedResult);
    }

    [Fact]
    public void can_use_null_as_default_value()
    {
        var schema = Schema.For(@"
                input HumanInput {
                  name: String!
                  homePlanet: String = null
                }

                type Human {
                  id: String!
                }

                type Mutation {
                  createHuman(human: HumanInput!): Human
                }
            ");

        var type = (InputObjectGraphType)schema.AllTypes.First(t => t.Name == "HumanInput");
        type.GetField("homePlanet").DefaultValue.ShouldBeNull();
    }

    [Fact]
    public async Task can_use_usercontext_with_params()
    {
        var schema = Schema.For(@"
                type Query {
                  userContextWithParam(id: String): String
                }
            ", _ => _.Types.Include<ParametersType>());

        var result = await ExecuteAsync(schema, _ =>
        {
            _.Query = @"{ userContextWithParam(id: ""abcd"") }";
            _.UserContext = new MyUserContext { Name = "Quinn" };
        }).ConfigureAwait(false);

        var expectedResult = CreateQueryResult(@"{ ""userContextWithParam"": ""Quinn abcd"" }");
        var serializedExpectedResult = Serializer.Serialize(expectedResult);

        result.ShouldBe(serializedExpectedResult);
    }

    [Fact]
    public async Task can_use_context_source_usercontext()
    {
        var schema = Schema.For(@"
                type Query {
                  three: Boolean
                }
            ", _ => _.Types.Include<ParametersType>());

        var result = await ExecuteAsync(schema, _ =>
        {
            _.Query = @"{ three }";
            _.Root = new { Hello = "World" };
            _.UserContext = new MyUserContext { Name = "Quinn" };
        }).ConfigureAwait(false);

        var expectedResult = CreateQueryResult(@"{ ""three"": true }");
        var serializedExpectedResult = Serializer.Serialize(expectedResult);

        result.ShouldBe(serializedExpectedResult);
    }

    [Fact]
    public async Task can_use_context_source_usercontext_with_params()
    {
        var schema = Schema.For(@"
                type Query {
                  four(id: Int): Boolean
                }
            ", _ => _.Types.Include<ParametersType>());

        var result = await ExecuteAsync(schema, _ =>
        {
            _.Query = @"{ four(id: 123) }";
            _.Root = new { Hello = "World" };
            _.UserContext = new MyUserContext { Name = "Quinn" };
        }).ConfigureAwait(false);

        var expectedResult = CreateQueryResult(@"{ ""four"": true }");
        var serializedExpectedResult = Serializer.Serialize(expectedResult);

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
                    post(id: ID!, unused: Long!): Post
                }
                type Query {
                    blog(id: ID!): Blog
                }
            ";

        Builder.Types.Include<BlogQueryType>();
        Builder.Types.Include<Blog>();

        var query = @"query Posts($blogId: ID!, $postId: ID!){ blog(id: $blogId){ title post(id: $postId, unused: 0) { id title } } }";
        var expected = @"{ ""blog"": { ""title"": ""New blog"", ""post"": { ""id"" : ""1"", ""title"": ""Post One"" } } }";
        var variables = @"{ ""blogId"": ""1"", ""postId"": ""1"" }";

        AssertQuery(_ =>
        {
            _.Query = query;
            _.Definitions = defs;
            _.ExpectedResult = expected;
            _.Variables = variables;
        });
    }

    [Fact]
    public void does_not_require_scalar_fields_to_be_defined()
    {
        var defs = @"
                type Person {
                    name: String!
                    age: Int!
                }
                type Query {
                    me: Person
                }
            ";

        Builder.Types.Include<PeopleQueryType>();
        Builder.Types.Include<PersonQueryType>();

        var query = @"{ me { name age } }";
        var expected = @"{ ""me"": { ""name"": ""Quinn"", ""age"": 100 } }";

        AssertQuery(_ =>
        {
            _.Query = query;
            _.Definitions = defs;
            _.ExpectedResult = expected;
        });
    }

    [Fact]
    public async Task resolves_union_references_when_union_defined_first()
    {
        var schema = Schema.For(@"
                union Pet = Dog | Cat

                enum PetKind {
                    CAT
                    DOG
                }

                type Query {
                    pet(type: PetKind = DOG): Pet
                }

                type Dog {
                    name: String!
                }

                type Cat {
                    name: String!
                }
            ", _ =>
        {
            _.Types.For("Dog").IsTypeOf<Dog>();
            _.Types.For("Cat").IsTypeOf<Cat>();
            _.Types.Include<PetQueryType>();
        });

        var result = await ExecuteAsync(schema, _ => _.Query = @"{ pet { ... on Dog { name } } }").ConfigureAwait(false);

        var expected = @"{ ""pet"": { ""name"" : ""Eli"" } }";
        var expectedResult = CreateQueryResult(expected);
        var serializedExpectedResult = Serializer.Serialize(expectedResult);

        result.ShouldBe(serializedExpectedResult);
    }

    private async Task<string> ExecuteAsync(ISchema schema, Action<ExecutionOptions> configure)
    {
        var executor = new DocumentExecuter();
        var result = await executor.ExecuteAsync(options =>
        {
            options.Schema = schema;
            configure(options);
        }).ConfigureAwait(false);

        return Serializer.Serialize(result);
    }
}

public class Person
{
    public string Name { get; set; }
}

[GraphQLMetadata("Person")]
public class PersonQueryType
{
    public int Age()
    {
        return 100;
    }
}

[GraphQLMetadata("Query")]
public class PeopleQueryType
{
    public Person Me()
    {
        return new Person { Name = "Quinn" };
    }
}

public static class PostData
{
    public static readonly List<Post> Posts = new List<Post>
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "for tests")]
    public Post Post(string id, long unused)
    {
        return PostData.Posts.FirstOrDefault(x => x.Id == id);
    }
}

[GraphQLMetadata("Query")]
public class BlogQueryType
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "for tests")]
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

internal abstract class Pet
{
    public string Name { get; set; }
}

internal class Dog : Pet
{
    public bool Barks { get; set; }
}

internal class Cat : Pet
{
    public bool Meows { get; set; }
}

internal enum PetKind
{
    Cat,
    Dog
}

[GraphQLMetadata("Query")]
internal class PetQueryType
{
    public Pet Pet(PetKind type)
    {
        if (type == PetKind.Dog)
        {
            return new Dog { Name = "Eli", Barks = true };
        }

        return new Cat { Name = "Biscuit", Meows = true };
    }
}

[GraphQLMetadata("Query")]
internal class ParametersType
{
    public bool Source([FromSource] object source) => source != null;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "for tests")]
    public string Resolve(IResolveFieldContext context) => "Resolved";

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "for tests")]
    public string ResolveWithParam(IResolveFieldContext context, string id) => $"Resolved {id}";

    public string UserContext([FromUserContext] MyUserContext context) => context.Name;

    public string UserContextWithParam([FromUserContext] MyUserContext context, string id) => $"{context.Name} {id}";

    public bool Three(IResolveFieldContext resolveContext, [FromSource] object source, [FromUserContext] MyUserContext context)
    {
        return resolveContext != null && context != null && source != null;
    }

    public bool Four(IResolveFieldContext resolveContext, [FromSource] object source, [FromUserContext] MyUserContext context, int id)
    {
        return resolveContext != null && context != null && source != null && id != 0;
    }
}

internal class MyUserContext : Dictionary<string, object>
{
    public string Name { get; set; }
}
internal class ChildMyUserContext : MyUserContext
{
}
