using GraphQL.Builders;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;
using Moq;

namespace GraphQL.MicrosoftDI.Tests;

public class ConnectionAdapterTests
{
    [Fact]
    public void NullBaseContextThrows()
    {
        Should.Throw<ArgumentNullException>(() => new ScopedResolveConnectionContextAdapter<string>(null, null));
    }

    [Fact]
    public void AllowNullProvider()
    {
        var adapter = new ScopedResolveConnectionContextAdapter<object>(new ResolveConnectionContext<object>(new ResolveFieldContext<object>(), false, null), null);
        adapter.RequestServices.ShouldBeNull();
    }

    [Fact]
    public void Passthrough()
    {
        var rccMock = new Mock<IResolveConnectionContext<string>>(MockBehavior.Strict);
        rccMock.SetupGet(x => x.Arguments).Returns(new Dictionary<string, ArgumentValue>() { { "6", default } });
        rccMock.SetupGet(x => x.ArrayPool).Returns(Mock.Of<IExecutionArrayPool>());
        rccMock.SetupGet(x => x.CancellationToken).Returns((CancellationToken)default);
        rccMock.SetupGet(x => x.Document).Returns(new GraphQLDocument());
        rccMock.SetupGet(x => x.Errors).Returns(new ExecutionErrors());
        rccMock.SetupGet(x => x.InputExtensions).Returns(new Dictionary<string, object>() { { "7", new object() } });
        rccMock.SetupGet(x => x.OutputExtensions).Returns(new Dictionary<string, object>() { { "1", new object() } });
        rccMock.SetupGet(x => x.FieldAst).Returns(new GraphQLField { Name = new GraphQLName("test") });
        rccMock.SetupGet(x => x.FieldDefinition).Returns(new FieldType());
        rccMock.SetupGet(x => x.Metrics).Returns(new Instrumentation.Metrics());
        rccMock.SetupGet(x => x.Operation).Returns(new GraphQLOperationDefinition());
        rccMock.SetupGet(x => x.ParentType).Returns(Mock.Of<IObjectGraphType>());
        rccMock.SetupGet(x => x.Path).Returns(new object[] { "5" });
        rccMock.SetupGet(x => x.RequestServices).Returns(Mock.Of<IServiceProvider>());
        rccMock.SetupGet(x => x.ResponsePath).Returns(new object[] { "4" });
        rccMock.SetupGet(x => x.RootValue).Returns(new object());
        rccMock.SetupGet(x => x.Schema).Returns(Mock.Of<ISchema>());
        rccMock.SetupGet(x => x.Source).Returns("hello");
        rccMock.SetupGet(x => x.SubFields).Returns(new Dictionary<string, (GraphQLField, FieldType)>());
        rccMock.SetupGet(x => x.UserContext).Returns(new Dictionary<string, object>() { { "3", new object() } });
        rccMock.SetupGet(x => x.Variables).Returns(new Variables());
        rccMock.SetupGet(x => x.First).Returns(10);
        rccMock.SetupGet(x => x.Last).Returns(11);
        rccMock.SetupGet(x => x.After).Returns("12");
        rccMock.SetupGet(x => x.Before).Returns("13");
        rccMock.SetupGet(x => x.IsUnidirectional).Returns(true);
        rccMock.SetupGet(x => x.PageSize).Returns(14);
        var rcc = rccMock.Object;
        var rs = Mock.Of<IServiceProvider>();
        var mocked = new ScopedResolveConnectionContextAdapter<object>(rcc, rs);
        mocked.IsUnidirectional.ShouldBe(rcc.IsUnidirectional);
        mocked.PageSize.ShouldBe(rcc.PageSize);
        mocked.Arguments.ShouldBe(rcc.Arguments);
        mocked.ArrayPool.ShouldBe(rcc.ArrayPool);
        mocked.CancellationToken.ShouldBe(rcc.CancellationToken);
        mocked.Document.ShouldBe(rcc.Document);
        mocked.Errors.ShouldBe(rcc.Errors);
        mocked.InputExtensions.ShouldBe(rcc.InputExtensions);
        mocked.OutputExtensions.ShouldBe(rcc.OutputExtensions);
        mocked.FieldAst.ShouldBe(rcc.FieldAst);
        mocked.FieldDefinition.ShouldBe(rcc.FieldDefinition);
        mocked.Metrics.ShouldBe(rcc.Metrics);
        mocked.Operation.ShouldBe(rcc.Operation);
        mocked.ParentType.ShouldBe(rcc.ParentType);
        mocked.Path.ShouldBe(rcc.Path);
        mocked.RequestServices.ShouldBe(rs);
        mocked.ResponsePath.ShouldBe(rcc.ResponsePath);
        mocked.RootValue.ShouldBe(rcc.RootValue);
        mocked.Schema.ShouldBe(rcc.Schema);
        mocked.Source.ShouldBe(rcc.Source);
        mocked.SubFields.ShouldBe(rcc.SubFields);
        mocked.UserContext.ShouldBe(rcc.UserContext);
        mocked.Variables.ShouldBe(rcc.Variables);
        mocked.First.ShouldBe(rcc.First);
        mocked.Last.ShouldBe(rcc.Last);
        mocked.After.ShouldBe(rcc.After);
        mocked.Before.ShouldBe(rcc.Before);
        mocked.IsUnidirectional.ShouldBe(rcc.IsUnidirectional);
        mocked.PageSize.ShouldBe(rcc.PageSize);
    }
}
