using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;
using Moq;

namespace GraphQL.MicrosoftDI.Tests;

public class AdapterTests
{
    [Fact]
    public void NullBaseContextThrows()
    {
        Should.Throw<ArgumentNullException>(() => new ScopedResolveFieldContextAdapter<string>(null, null));
    }

    [Fact]
    public void AllowNullProvider()
    {
        var adapter = new ScopedResolveFieldContextAdapter<object>(new ResolveFieldContext(), null);
        adapter.RequestServices.ShouldBeNull();
    }

    [Fact]
    public void InvalidSourceTypeThrows()
    {
        var rfc = new ResolveFieldContext<string>
        {
            Source = "hello"
        };
        Should.Throw<ArgumentException>(() => new ScopedResolveFieldContextAdapter<int>(rfc, null));
    }

    [Fact]
    public void SourceNullThrowsForValueTypes()
    {
        Should.Throw<ArgumentException>(() => new ScopedResolveFieldContextAdapter<int>(new ResolveFieldContext(), null));
    }

    [Fact]
    public void SourceNullNullableTypes()
    {
        var adapter = new ScopedResolveFieldContextAdapter<int?>(new ResolveFieldContext(), null);
        adapter.Source.ShouldBeNull();
    }

    [Fact]
    public void Passthrough()
    {
        var rfc = new ResolveFieldContext<string>
        {
            Arguments = new Dictionary<string, ArgumentValue>() { { "6", default } },
            ArrayPool = Mock.Of<IExecutionArrayPool>(),
            CancellationToken = default,
            Document = new GraphQLDocument(),
            Errors = new ExecutionErrors(),
            InputExtensions = new Dictionary<string, object>() { { "7", new object() } },
            OutputExtensions = new Dictionary<string, object>() { { "1", new object() } },
            FieldAst = new GraphQLField { Name = new GraphQLName("test") },
            FieldDefinition = new FieldType(),
            Metrics = new Instrumentation.Metrics(),
            Operation = new GraphQLOperationDefinition(),
            ParentType = Mock.Of<IObjectGraphType>(),
            Path = new object[] { "5" },
            RequestServices = Mock.Of<IServiceProvider>(),
            ResponsePath = new object[] { "4" },
            RootValue = new object(),
            Schema = Mock.Of<ISchema>(),
            Source = "hello",
            SubFields = new Dictionary<string, (GraphQLField, FieldType)>(),
            UserContext = new Dictionary<string, object>() { { "3", new object() } },
            Variables = new Variables(),
        };
        var rs = Mock.Of<IServiceProvider>();
        var mocked = new ScopedResolveFieldContextAdapter<object>(rfc, rs);
        mocked.Arguments.ShouldBe(rfc.Arguments);
        mocked.ArrayPool.ShouldBe(rfc.ArrayPool);
        mocked.CancellationToken.ShouldBe(rfc.CancellationToken);
        mocked.Document.ShouldBe(rfc.Document);
        mocked.Errors.ShouldBe(rfc.Errors);
        mocked.InputExtensions.ShouldBe(rfc.InputExtensions);
        mocked.OutputExtensions.ShouldBe(rfc.OutputExtensions);
        mocked.FieldAst.ShouldBe(rfc.FieldAst);
        mocked.FieldDefinition.ShouldBe(rfc.FieldDefinition);
        mocked.Metrics.ShouldBe(rfc.Metrics);
        mocked.Operation.ShouldBe(rfc.Operation);
        mocked.ParentType.ShouldBe(rfc.ParentType);
        mocked.Path.ShouldBe(rfc.Path);
        mocked.RequestServices.ShouldBe(rs);
        mocked.ResponsePath.ShouldBe(rfc.ResponsePath);
        mocked.RootValue.ShouldBe(rfc.RootValue);
        mocked.Schema.ShouldBe(rfc.Schema);
        mocked.Source.ShouldBe(rfc.Source);
        ((IResolveFieldContext)mocked).Source.ShouldBe(((IResolveFieldContext)rfc).Source);
        mocked.SubFields.ShouldBe(rfc.SubFields);
        mocked.UserContext.ShouldBe(rfc.UserContext);
        mocked.Variables.ShouldBe(rfc.Variables);
    }
}
