using System.Security.Claims;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;

namespace GraphQL.Tests.Execution;

public class ResolveFieldContextTests
{
    private readonly ResolveFieldContext _context;

    public ResolveFieldContextTests()
    {
        var fieldDef = new FieldType()
        {
            Arguments = new QueryArguments
            {
                new QueryArgument(new IntGraphType()) { Name = "int" },
                new QueryArgument(new LongGraphType()) { Name = "long" },
                new QueryArgument(new FloatGraphType()) { Name = "float" },
                new QueryArgument(new StringGraphType()) { Name = "string" },
                new QueryArgument(new ListGraphType(new StringGraphType())) { Name = "stringlist" },
                new QueryArgument(new EnumerationGraphType<SomeEnum>()) { Name = "enum" },
            },
        };
        _context = new ResolveFieldContext
        {
            Arguments = new Dictionary<string, ArgumentValue>(),
            Errors = new ExecutionErrors(),
            OutputExtensions = new Dictionary<string, object?>(),
            FieldDefinition = fieldDef,
        };
    }

    [Fact]
    public void argument_converts_int_to_long()
    {
        const int val = 1;
        _context.Arguments!["int"] = new ArgumentValue(val, ArgumentSource.Literal);
        long result = _context.GetArgument<long>("int");
        result.ShouldBe(1);
    }

    [Fact]
    public void argument_converts_long_to_int()
    {
        const long val = 1;
        _context.Arguments!["long"] = new ArgumentValue(val, ArgumentSource.Literal);
        int result = _context.GetArgument<int>("long");
        result.ShouldBe(1);
    }

    [Fact]
    public void long_to_int_should_throw_for_out_of_range()
    {
        const long val = 89429901947254093;
        _context.Arguments!["long"] = new ArgumentValue(val, ArgumentSource.Literal);
        Should.Throw<OverflowException>(() => _context.GetArgument<int>("long"));
    }

    [Fact]
    public void argument_returns_boxed_string_uncast()
    {
        const string val = "one";
        _context.Arguments!["string"] = new ArgumentValue(val, ArgumentSource.Literal);
        object result = _context.GetArgument<object>("string");
        result.ShouldBe("one");
    }

    [Fact]
    public void argument_returns_long()
    {
        const long val = 1000000000000001;
        _context.Arguments!["long"] = new ArgumentValue(val, ArgumentSource.Literal);
        long result = _context.GetArgument<long>("long");
        result.ShouldBe(1000000000000001);
    }

    [Fact]
    public void argument_returns_enum()
    {
        const SomeEnum val = SomeEnum.Two;
        _context.Arguments!["enum"] = new ArgumentValue(val, ArgumentSource.Literal);
        var result = _context.GetArgument<SomeEnum>("enum");
        result.ShouldBe(SomeEnum.Two);
    }

    [Fact]
    public void argument_returns_enum_from_string()
    {
        const string val = "two";
        _context.Arguments!["enum"] = new ArgumentValue(val, ArgumentSource.Literal);
        var result = _context.GetArgument<SomeEnum>("enum");
        result.ShouldBe(SomeEnum.Two);
    }

    [Fact]
    public void argument_returns_enum_from_number()
    {
        const int val = 1;
        _context.Arguments!["enum"] = new ArgumentValue(val, ArgumentSource.Literal);
        var result = _context.GetArgument<SomeEnum>("enum");
        result.ShouldBe(SomeEnum.Two);
    }

    [Fact]
    public void argument_returns_default_when_missing()
    {
        _context.GetArgument<string>("string").ShouldBeNull();
    }

    [Fact]
    public void argument_returns_provided_default_when_missing()
    {
        _context.GetArgument("string", "foo").ShouldBe("foo");
    }

    [Fact]
    public void argument_returns_list_from_array()
    {
        _context.Arguments = new Dictionary<string, ArgumentValue>
        {
            { "stringlist", new ArgumentValue(new string[] { "one", "two"}, ArgumentSource.Literal) }
        };
        var result = _context.GetArgument<List<string>>("stringlist");
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result[0].ShouldBe("one");
        result[1].ShouldBe("two");
    }

    [Fact]
    public void resolveFieldContextAdapter_throws_error_when_null()
    {
        Should.Throw<ArgumentNullException>(() => _ = new ResolveFieldContextAdapter<object>(null!));
    }

    [Fact]
    public void resolveFieldContextAdapter_throws_error_if_invalid_type()
    {
        var context = new ResolveFieldContext { Source = "test" };
        Should.Throw<ArgumentException>(() => _ = new ResolveFieldContextAdapter<int>(context));
    }

    [Fact]
    public void resolveFieldContextAdapter_accepts_null_sources_ref()
    {
        var context = new ResolveFieldContext();
        var adapter = new ResolveFieldContextAdapter<string>(context);
        adapter.Source.ShouldBe(null);
    }

    [Fact]
    public void resolveFieldContextAdapter_accepts_null_sources_nullable()
    {
        var context = new ResolveFieldContext();
        var adapter = new ResolveFieldContextAdapter<int?>(context);
        adapter.Source.ShouldBe(null);
    }

    [Fact]
    public void resolveFieldContextAdapter_throws_error_for_null_values()
    {
        var context = new ResolveFieldContext();
        Should.Throw<ArgumentException>(() => _ = new ResolveFieldContextAdapter<int>(context));
    }

    [Fact]
    public void GetSetExtension_Should_Throw_On_Null()
    {
        IResolveFieldContext context = null!;
        Should.Throw<ArgumentNullException>(() => context.GetOutputExtension("e"));
        Should.Throw<ArgumentNullException>(() => context.GetInputExtension("e"));
        Should.Throw<ArgumentNullException>(() => context.SetOutputExtension("e", 1));

        context = new ResolveFieldContext();
        context.GetOutputExtension("a").ShouldBe(null);
        context.GetOutputExtension("a.b.c.d").ShouldBe(null);
        context.GetInputExtension("a").ShouldBe(null);
        context.GetInputExtension("a.b.c.d").ShouldBe(null);
        Should.Throw<ArgumentException>(() => context.SetOutputExtension("e", 1));
    }

    [Fact]
    public void GetSetExtension_Should_Get_And_Set_Values()
    {
        _context.GetOutputExtension("a").ShouldBe(null);
        _context.GetOutputExtension("a.b.c.d").ShouldBe(null);

        _context.SetOutputExtension("a", 5);
        _context.GetOutputExtension("a").ShouldBe(5);

        _context.SetOutputExtension("a.b.c.d", "value");
        _context.GetOutputExtension("a.b.c.d").ShouldBe("value");
        var d = _context.GetOutputExtension("a.b").ShouldBeOfType<Dictionary<string, object?>>();
        d.Count.ShouldBe(1);

        _context.SetOutputExtension("a.b.c", "override");
        _context.GetOutputExtension("a.b.c.d").ShouldBe(null);
    }

    [Fact]
    public void GetInputExtension_Should_Return_Value()
    {
        var context = new ResolveFieldContext
        {
            InputExtensions = new Dictionary<string, object?>
            {
                ["a"] = 1,
                ["b"] = new Dictionary<string, object?>
                {
                    ["c"] = true
                },
                ["d"] = new object()
            }
        };
        context.GetInputExtension("a").ShouldBe(1);
        context.GetInputExtension("b.c").ShouldBe(true);
        context.GetInputExtension("a.x").ShouldBe(null);
        context.GetInputExtension("b.x").ShouldBe(null);
        context.GetInputExtension("d").ShouldBeOfType<object>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task User_Returns_ClaimsPrincipal(bool isAuthenticated)
    {
        var schema = new Schema();
        var queryType = new ObjectGraphType();
        queryType.Field<BooleanGraphType>("IsAuthenticated")
            .Resolve(context => context.User.ShouldNotBeNull().Identity.ShouldNotBeNull().IsAuthenticated);
        schema.Query = queryType;
        var executer = new DocumentExecuter();
        var options = new ExecutionOptions
        {
            Schema = schema,
            Query = "{ isAuthenticated }",
            ValidationRules = DocumentValidator.CoreRules.Append(new VerifyUserValidationRule { ShouldBeAuthenticated = isAuthenticated }),
            User = new ClaimsPrincipal(new ClaimsIdentity(isAuthenticated ? "Bearer" : null)),
        };
        options.Listeners.Add(new VerifyUserDocumentListener { ShouldBeAuthenticated = isAuthenticated });
        var result = await executer.ExecuteAsync(options);
        string resultText = new SystemTextJson.GraphQLSerializer().Serialize(result);
        resultText.ShouldBe(isAuthenticated ? """{"data":{"isAuthenticated":true}}""" : """{"data":{"isAuthenticated":false}}""");
    }

    private class VerifyUserDocumentListener : DocumentExecutionListenerBase
    {
        public bool ShouldBeAuthenticated { get; set; }

        public override Task BeforeExecutionAsync(IExecutionContext context)
        {
            context.User.ShouldNotBeNull().Identity.ShouldNotBeNull().IsAuthenticated.ShouldBe(ShouldBeAuthenticated);
            return Task.CompletedTask;
        }
    }

    private class VerifyUserValidationRule : ValidationRuleBase
    {
        public bool ShouldBeAuthenticated { get; set; }

        public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
        {
            context.User.ShouldNotBeNull().Identity.ShouldNotBeNull().IsAuthenticated.ShouldBe(ShouldBeAuthenticated);
            return default;
        }
    }

    [Fact]
    public async Task ExecutionError_Should_Be_Thread_Safe()
    {
        using var e = new CountdownEvent(2);

        var t1 = Task.Run(() =>
        {
            e.Signal();
            e.Wait();
            for (int i = 0; i < 5; ++i)
                _context.Errors.Add(new ExecutionError("test"));
        });
        var t2 = Task.Run(() =>
        {
            e.Signal();
            e.Wait();
            for (int i = 0; i < 5; ++i)
                _context.Errors.Add(new ExecutionError("test"));
        });

        await Task.WhenAll(t1, t2);

        _context.Errors.Count.ShouldBe(10);
    }

    private enum SomeEnum
    {
        One,
        Two
    }
}
