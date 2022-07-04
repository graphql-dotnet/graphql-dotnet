using GraphQL.StarWars.Types;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Utilities;

public class SchemaBuilderTests
{
    [Fact]
    public void should_set_query_by_name()
    {
        var definitions = @"
                type Query {
                    id: String
                }
            ";

        var schema = Schema.For(definitions);
        schema.Initialize();

        var query = schema.Query;
        query.ShouldNotBeNull();
        query.Name.ShouldBe("Query");
        query.Fields.Count.ShouldBe(1);

        query.Fields.Single().Name.ShouldBe("id");
    }

    [Fact]
    public void should_set_mutation_by_name()
    {
        var definitions = @"
                type Mutation {
                    mutate: String
                }
            ";

        var schema = Schema.For(definitions);
        schema.Initialize();

        var mutation = schema.Mutation;
        mutation.ShouldNotBeNull();
        mutation.Name.ShouldBe("Mutation");
        mutation.Fields.Count.ShouldBe(1);

        mutation.Fields.Single().Name.ShouldBe("mutate");
    }

    [Fact]
    public void should_set_subscription_by_name()
    {
        var definitions = @"
                type Subscription {
                    subscribe: String
                }
            ";

        var schema = Schema.For(definitions);
        schema.Initialize();

        var subscription = schema.Subscription;
        subscription.ShouldNotBeNull();
        subscription.Name.ShouldBe("Subscription");
        subscription.Fields.Count.ShouldBe(1);

        subscription.Fields.Single().Name.ShouldBe("subscribe");
    }

    [Fact]
    public void configures_schema_from_schema_type()
    {
        var definitions = @"
                type MyQuery {
                    id: String
                }

                type MyMutation {
                    mutate: String
                }

                type MySubscription {
                    subscribe: String
                }

                schema {
                  query: MyQuery
                  mutation: MyMutation
                  subscription: MySubscription
                }
            ";

        var schema = Schema.For(definitions);
        schema.Initialize();

        var query = schema.Query;
        query.ShouldNotBeNull();
        query.Name.ShouldBe("MyQuery");

        var mutation = schema.Mutation;
        mutation.ShouldNotBeNull();
        mutation.Name.ShouldBe("MyMutation");

        var subscription = schema.Subscription;
        subscription.ShouldNotBeNull();
        subscription.Name.ShouldBe("MySubscription");
    }

    private enum TestEnum
    {
        ASC,
        DESC
    }

    [Fact]
    public void configures_schema_from_schema_type_and_directives()
    {
        var definitions = @"
                type MyQuery  {
                    id: String
                }

                type MyMutation @requireAuth(role: ""Admin"") {
                    mutate: String
                }

                type MySubscription @requireAuth {
                    subscribe: String @traits(volatile: true, documented: false, enumerated: DESC) @some @some
                }

                schema @public {
                  query: MyQuery
                  mutation: MyMutation
                  subscription: MySubscription
                }
            ";

        var schema = Schema.For(definitions);
        schema.Directives.Register(new Directive("public", DirectiveLocation.Schema));
        schema.Directives.Register(new Directive("requireAuth", DirectiveLocation.Object) { Arguments = new QueryArguments(new QueryArgument<StringGraphType> { Name = "role" }) });
        schema.Directives.Register(new Directive("traits", DirectiveLocation.FieldDefinition) { Arguments = new QueryArguments(new QueryArgument<NonNullGraphType<BooleanGraphType>> { Name = "volatile" }, new QueryArgument<BooleanGraphType> { Name = "documented" }, new QueryArgument<EnumerationGraphType<TestEnum>> { Name = "enumerated" }) });
        schema.Directives.Register(new Directive("some", DirectiveLocation.FieldDefinition) { Repeatable = true });
        schema.Initialized.ShouldBe(false);
        schema.Initialize();

        schema.HasAppliedDirectives().ShouldBeTrue();
        schema.GetAppliedDirectives().Count.ShouldBe(1);
        schema.GetAppliedDirectives().Find("public").ShouldNotBeNull();
        schema.GetAppliedDirectives().Find("public").ArgumentsCount.ShouldBe(0);
        schema.GetAppliedDirectives().Find("public").List.ShouldBeNull();

        var query = schema.Query;
        query.ShouldNotBeNull();
        query.Name.ShouldBe("MyQuery");
        query.HasAppliedDirectives().ShouldBeFalse();
        query.GetAppliedDirectives().ShouldBeNull();

        var mutation = schema.Mutation;
        mutation.ShouldNotBeNull();
        mutation.Name.ShouldBe("MyMutation");
        mutation.HasAppliedDirectives().ShouldBeTrue();
        mutation.GetAppliedDirectives().Count.ShouldBe(1);
        mutation.GetAppliedDirectives().Find("requireAuth").ShouldNotBeNull();
        mutation.GetAppliedDirectives().Find("requireAuth").List.Count.ShouldBe(1);
        mutation.GetAppliedDirectives().Find("requireAuth").List[0].Name.ShouldBe("role");
        mutation.GetAppliedDirectives().Find("requireAuth").List[0].Value.ShouldBe("Admin");

        var subscription = schema.Subscription;
        subscription.ShouldNotBeNull();
        subscription.Name.ShouldBe("MySubscription");
        subscription.GetAppliedDirectives().Count.ShouldBe(1);
        subscription.GetAppliedDirectives().Find("requireAuth").ShouldNotBeNull();
        subscription.GetAppliedDirectives().Find("requireAuth").ArgumentsCount.ShouldBe(0);
        subscription.GetAppliedDirectives().Find("requireAuth").List.ShouldBeNull();

        var field = subscription.Fields.Find("subscribe");
        field.ShouldNotBeNull();
        field.GetAppliedDirectives().Count.ShouldBe(3);
        field.GetAppliedDirectives().Find("traits").ShouldNotBeNull();
        field.GetAppliedDirectives().Find("traits").List.Count.ShouldBe(3);
        field.GetAppliedDirectives().Find("traits").List[0].Name.ShouldBe("volatile");
        field.GetAppliedDirectives().Find("traits").List[0].Value.ShouldBe(true);
        field.GetAppliedDirectives().Find("traits").List[1].Name.ShouldBe("documented");
        field.GetAppliedDirectives().Find("traits").List[1].Value.ShouldBe(false);
        field.GetAppliedDirectives().Find("traits").List[2].Name.ShouldBe("enumerated");
        field.GetAppliedDirectives().Find("traits").List[2].Value.ShouldBe("DESC");
        field.GetAppliedDirectives().Find("some").ShouldNotBeNull();
        field.GetAppliedDirectives().Find("some").ArgumentsCount.ShouldBe(0);
    }

    [Fact]
    public void builds_type_with_arguments()
    {
        var definitions = @"
                type Query {
                  """"""
                  Post description
                  """"""
                  post(""ID description"" id: ID = 1, ""Val description"" val: String): String
                }
            ";

        var schema = Schema.For(definitions, builder => builder.Types.For("Query").FieldFor("post").ArgumentFor("id").Description = "Some argument");
        schema.Initialize();

        var query = schema.Query;
        query.Fields.Count.ShouldBe(1);

        var field = query.Fields.Single();
        field.Name.ShouldBe("post");
        field.Arguments.Count.ShouldBe(2);
        field.ResolvedType.Name.ShouldBe("String");
        field.Description.ShouldBe("Post description");

        var arg = field.Arguments.First();
        arg.Name.ShouldBe("id");
        arg.DefaultValue.ShouldBe(1);
        arg.ResolvedType.Name.ShouldBe("ID");
        arg.Description.ShouldBe("Some argument");

        arg = field.Arguments.Last();
        arg.Name.ShouldBe("val");
        arg.ResolvedType.Name.ShouldBe("String");
        arg.Description.ShouldBe("Val description");
    }

    [Fact]
    public void builds_type_with_arguments_with_dependent_default_values()
    {
        var definitions = @"
                type Query {
                  post(arg: [SomeInputType] = [{ name: ""John"" }]): String
                }
                input SomeInputType {
                  id: ID! = 1
                  name: String!
                }
            ";

        var schema = Schema.For(definitions);
        schema.Initialize();

        var query = schema.Query;
        query.Fields.Count.ShouldBe(1);

        var field = query.Fields.Single();
        field.Name.ShouldBe("post");
        field.Arguments.Count.ShouldBe(1);

        var arg = field.Arguments.First();
        arg.Name.ShouldBe("arg");
        var list = arg.DefaultValue.ShouldBeAssignableTo<IEnumerable<object>>();
        list.Count().ShouldBe(1);
        var item = list.First().ShouldBeAssignableTo<IDictionary<string, object>>();
        item.Count.ShouldBe(2);
        item["name"].ShouldBeOfType<string>().ShouldBe("John");
        item["id"].ShouldBeOfType<int>().ShouldBe(1);
    }

    [Fact]
    public void builds_interface()
    {
        var definitions = @"
                """"""
                Example description
                """"""
                interface Pet {
                    """"""
                    ID description
                    """"""
                    id: ID
                }
            ";

        var schema = Schema.For(definitions);
        schema.Initialize();

        var type = schema.AllTypes["Pet"] as InterfaceGraphType;
        type.ShouldNotBeNull();
        type.Description.ShouldBe("Example description");
        type.Fields.Count.ShouldBe(1);

        var field = type.Fields.Single();
        field.Name.ShouldBe("id");
        field.ResolvedType.Name.ShouldBe("ID");
        field.Description.ShouldBe("ID description");
    }

    [Fact]
    public void builds_enum()
    {
        var definitions = @"
                """"""
                Example description
                """"""
                enum PetKind {
                    """"""
                    Cat description
                    """"""
                    CAT
                    DOG
                }
            ";

        var schema = Schema.For(definitions);
        schema.Initialize();

        var type = schema.AllTypes["PetKind"] as EnumerationGraphType;
        type.ShouldNotBeNull();
        type.Description.ShouldBe("Example description");

        type.Values.Select(x => x.Name).ShouldBe(new[] { "CAT", "DOG" });
        type.Values.Select(x => x.Value.ToString()).ShouldBe(new[] { "CAT", "DOG" });
        type.Values.Select(x => x.Description).ShouldBe(new[] { "Cat description", null });
    }

    private enum PetKind
    {
        Cat,
        Dog
    }

    [Fact]
    public void builds_case_insensitive_typed_enum()
    {
        var definitions = @"
                enum PetKind {
                    CAT
                    DOG
                }
            ";

        var schema = Schema.For(definitions, c => c.Types.Include<PetKind>());
        schema.Initialize();

        var type = schema.AllTypes["PetKind"] as EnumerationGraphType;
        type.ShouldNotBeNull();

        type.Values.Select(x => x.Name).ShouldBe(new[] { "CAT", "DOG" });
        type.Values.Select(x => (PetKind)x.Value).ShouldBe(new[] { PetKind.Cat, PetKind.Dog });
    }

    [Fact]
    public void builds_scalars()
    {
        var definitions = @"
                scalar CustomScalar

                type Query {
                    search: CustomScalar
                }
            ";

        var customScalar = new CustomScalarType();

        var schema = Schema.For(definitions);
        schema.RegisterType(customScalar);
        schema.Initialize();

        var type = schema.AllTypes["CustomScalar"] as ScalarGraphType;
        type.ShouldNotBeNull();

        var query = schema.Query;
        query.ShouldNotBeNull();

        var field = query.Fields.First();
        field.ResolvedType.ShouldBeOfType<CustomScalarType>();
    }

    [Fact]
    public void references_other_types()
    {
        var definitions = @"
                type Post {
                  id: ID!
                  title: String
                  votes: Int
                }

                type Query {
                  posts: [Post]
                  post(id: ID = 1): Post
                }
            ";

        var schema = Schema.For(definitions, builder => builder.Types.For("Query").FieldFor("post").ArgumentFor("id").DefaultValue = 999);
        schema.Initialize();

        var query = schema.Query;

        query.ShouldNotBeNull();
        query.Name.ShouldBe("Query");
        query.Fields.Count.ShouldBe(2);

        var posts = query.Fields.First();
        posts.Name.ShouldBe("posts");
        posts.ResolvedType.ToString().ShouldBe("[Post]");
        query.Fields.Last().ResolvedType.Name.ShouldBe("Post");

        var post = schema.AllTypes["Post"] as IObjectGraphType;
        post.ShouldNotBeNull();
        post.Fields.Count.ShouldBe(3);

        var arg = query.Fields.Last().Arguments.Single();
        arg.Name.ShouldBe("id");
        arg.DefaultValue.ShouldBe(999);
        arg.Description.ShouldBeNull();
    }

    [Fact]
    public void builds_unions()
    {
        var definitions = @"
                type Human {
                    name: String
                }

                type Droid {
                    name: String
                }

                """"""
                Example description
                """"""
                union SearchResult = Human | Droid";

        var schema = Schema.For(definitions, _ =>
        {
            _.Types.For("Human").IsTypeOf<Human>();
            _.Types.For("Droid").IsTypeOf<Droid>();
        });

        schema.Initialize();

        var searchResult = schema.AllTypes["SearchResult"] as UnionGraphType;
        searchResult.Description.ShouldBe("Example description");
        searchResult.PossibleTypes.Select(x => x.Name).ShouldBe(new[] { "Human", "Droid" });
    }

    [Fact]
    public void builds_input_types()
    {
        var definitions = @"
                """"""
                Example description
                """"""
                input ReviewInput {
                  """"""
                  Stars description
                  """"""
                  stars: Int!
                  commentary: String
                }
            ";

        var schema = Schema.For(definitions);
        schema.Initialize();

        var input = schema.AllTypes["ReviewInput"] as InputObjectGraphType;
        input.ShouldNotBeNull();
        input.Description.ShouldBe("Example description");
        input.Fields.Count.ShouldBe(2);
        input.Fields.First().Description.ShouldBe("Stars description");
    }

    [Fact]
    public void builds_input_types_with_default_values()
    {
        var definitions = @"
                input ReviewInput {
                  stars: Int! = 23
                }
            ";

        var schema = Schema.For(definitions);
        schema.Initialize();

        var input = schema.AllTypes["ReviewInput"] as InputObjectGraphType;
        input.ShouldNotBeNull();
        input.Fields.Count.ShouldBe(1);
        input.Fields.First().DefaultValue.ShouldBe(23);
    }

    [Fact]
    public void builds_input_types_with_dependent_default_values()
    {
        var definitions = @"
                input SomeInputType1 {
                  test: SomeInputType2! = { arg1: 22 }
                }
                input SomeInputType2 {
                  arg1: Int
                  arg2: Int = 30
                }
            ";

        var schema = Schema.For(definitions);
        schema.Initialize();

        var inputType1 = schema.AllTypes["SomeInputType1"] as InputObjectGraphType;
        inputType1.ShouldNotBeNull();
        inputType1.Fields.Count.ShouldBe(1);
        inputType1.Fields.First().Name.ShouldBe("test");
        var value1 = inputType1.Fields.First().DefaultValue.ShouldBeAssignableTo<IDictionary<string, object>>();
        value1.ShouldContainKeyAndValue("arg1", 22);
        value1.ShouldContainKeyAndValue("arg2", 30);

        var inputType2 = schema.AllTypes["SomeInputType2"] as InputObjectGraphType;
        inputType2.ShouldNotBeNull();
        inputType2.Fields.Count.ShouldBe(2);
        inputType2.Fields.Find("arg1").ShouldNotBeNull().DefaultValue.ShouldBeNull();
        inputType2.Fields.Find("arg2").ShouldNotBeNull().DefaultValue.ShouldBe(30);
    }

    [Fact]
    public void input_types_default_value_loops_throw1()
    {
        var definitions = @"
                input SomeInputType1 {
                  test: SomeInputType1 = { }
                }
            ";

        var schema = Schema.For(definitions);
        Should.Throw<InvalidOperationException>(() => schema.Initialize()).Message.ShouldBe("Default values in input types cannot contain a circular dependency loop. Please resolve dependency loop between the following types: 'SomeInputType1'.");
    }

    [Fact]
    public void input_types_default_value_loops_throw2()
    {
        var definitions = @"
                input SomeInputType1 {
                  test: SomeInputType2 = { }
                }
                input SomeInputType2 {
                  test: SomeInputType3 = { }
                }
                input SomeInputType3 {
                  test: SomeInputType1 = { }
                }
            ";

        var schema = Schema.For(definitions);
        Should.Throw<InvalidOperationException>(() => schema.Initialize()).Message.ShouldBe("Default values in input types cannot contain a circular dependency loop. Please resolve dependency loop between the following types: 'SomeInputType3', 'SomeInputType2', 'SomeInputType1'.");
    }

    [Fact(Skip = "Not yet supported")]
    public void input_types_default_value_loops_pass()
    {
        // see: https://github.com/graphql-dotnet/graphql-dotnet/pull/2696#discussion_r764975585

        var definitions = @"
                input SomeInputType1 {
                  test: SomeInputType1 = { test: null }
                }
            ";

        var schema = Schema.For(definitions);
        schema.Initialize();
    }

    [Fact]
    public void builds_directives()
    {
        var definitions = @"
                """"""
                Example description
                """"""
                directive @myDirective(
                  if: Boolean!
                ) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT
            ";

        var schema = Schema.For(definitions);
        schema.Initialize();

        var directive = schema.Directives.Find("myDirective");
        directive.ShouldNotBeNull();
        directive.Description.ShouldBe("Example description");

        directive.Arguments.Count.ShouldBe(1);
        var argument = directive.Arguments.Find("if");
        argument.ResolvedType.ToString().ShouldBe("Boolean!");

        directive.Locations.ShouldBe(new[]
        {
            DirectiveLocation.Field,
            DirectiveLocation.FragmentSpread,
            DirectiveLocation.InlineFragment
        });
    }

    [Fact]
    public void custom_deprecation_on_type_field()
    {
        var definitions = @"
                type Query {
                  stars: Int @deprecated(reason: ""a reason"")
                }
            ";

        var schema = Schema.For(definitions);
        schema.Initialize();

        var type = schema.AllTypes["Query"] as IObjectGraphType;
        type.ShouldNotBeNull();
        type.Fields.Count.ShouldBe(1);
        type.Fields.Single().DeprecationReason.ShouldBe("a reason");
    }

    [Fact]
    public void default_deprecation_on_type_field()
    {
        var definitions = @"
                type Query {
                  stars: Int @deprecated
                }
            ";

        var schema = Schema.For(definitions);
        schema.Initialize();

        var type = schema.AllTypes["Query"] as IObjectGraphType;
        type.ShouldNotBeNull();
        type.Fields.Count.ShouldBe(1);
        type.Fields.Single().DeprecationReason.ShouldBe("No longer supported");
    }

    [Fact]
    public void deprecate_enum_value()
    {
        var definitions = @"
                enum PetKind {
                    CAT @deprecated(reason: ""dogs rule"")
                    DOG
                }
            ";

        var schema = Schema.For(definitions);
        schema.Initialize();

        var type = schema.AllTypes["PetKind"] as EnumerationGraphType;
        type.ShouldNotBeNull();

        var cat = type.Values.Single(x => x.Name == "CAT");
        cat.DeprecationReason.ShouldBe("dogs rule");
    }

    [Fact]
    public void deprecated_prefers_metadata_values()
    {
        var definitions = @"
                type Movie {
                  movies: Int @deprecated
                }
            ";

        var schema = Schema.For(definitions, _ => _.Types.Include<Movie>());
        schema.Initialize();

        var type = schema.AllTypes["Movie"] as IObjectGraphType;
        type.ShouldNotBeNull();
        type.Fields.Count.ShouldBe(1);
        type.Fields.Single().DeprecationReason.ShouldBe("my reason");
    }

    [Fact]
    public void build_extension_type()
    {
        var definitions = @"
                type Query {
                    author(id: Int): String
                }

                extend type Query {
                    book(id: Int): String
                }
            ";

        var schema = Schema.For(definitions);
        schema.Initialize();
        var type = schema.AllTypes["Query"] as IObjectGraphType;
        type.Fields.Count.ShouldBe(2);
    }

    [Fact]
    public void build_extension_type_out_of_order()
    {
        var definitions = @"
                extend type Query {
                    author(id: Int): String
                }

                type Query {
                    book(id: Int): String
                }
            ";

        var schema = Schema.For(definitions);
        schema.Initialize();
        var type = (IObjectGraphType)schema.AllTypes["Query"];
        type.Fields.Count.ShouldBe(2);
    }

    [Fact]
    public async Task builds_with_customized_clr_type()
    {
        var definitions = @"
                type Query {
                    test: MyTestType
                }

                type MyTestType {
                    myTestField: ID
                }
            ";

        var schema = Schema.For(definitions, b => b.Types.Include<TestType>());
        schema.Initialize();

        var graphType = schema.AllTypes["MyTestType"].ShouldNotBeNull().ShouldBeAssignableTo<ObjectGraphType>();
        graphType.Description.ShouldBe("Test1");
        graphType.DeprecationReason.ShouldBe("Test2");
        var fieldType = graphType.Fields.Find("myTestField").ShouldNotBeNull();
        fieldType.Description.ShouldBe("Test3");
        fieldType.DeprecationReason.ShouldBe("Test4");
        fieldType.ResolvedType.ShouldBeOfType<IdGraphType>();
        var context = new ResolveFieldContext() { Source = new TestType() };
        (await fieldType.Resolver.ResolveAsync(context).ConfigureAwait(false)).ShouldBeOfType<string>().ShouldBe("value");
    }

    [GraphQLMetadata("MyTestType", Description = "Test1", DeprecationReason = "Test2")]
    internal class TestType
    {
        [GraphQLMetadata("myTestField", Description = "Test3", DeprecationReason = "Test4")]
        public string Example() => "value";
    }

    internal class Movie
    {
        [GraphQLMetadata("movies", DeprecationReason = "my reason")]
        public int Movies() => 0;
    }

    internal class CustomScalarType : ScalarGraphType
    {
        public CustomScalarType()
        {
            Name = "CustomScalar";
        }

        public override object ParseValue(object value) => throw new System.NotImplementedException();

        public override object ParseLiteral(GraphQLValue value) => throw new System.NotImplementedException();
    }
}
