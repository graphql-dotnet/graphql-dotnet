using GraphQL.DI;
using GraphQL.Execution;
using Moq;

namespace GraphQL.DataLoader.Tests;

public class GraphQLBuilderTests
{
    [Fact]
    public void AddDataLoader()
    {
        var instance = new DataLoaderDocumentListener(new DataLoaderContextAccessor());
        var mockRegister = new Mock<IServiceRegister>(MockBehavior.Strict);
        var mockBuilder = new Mock<IGraphQLBuilder>(MockBehavior.Strict);
        var register = mockRegister.Object;
        var builder = mockBuilder.Object;
        mockBuilder.Setup(x => x.Services).Returns(register).Verifiable();
        mockRegister.Setup(x => x.Register(typeof(IDataLoaderContextAccessor), typeof(DataLoaderContextAccessor), ServiceLifetime.Singleton, false)).Returns(register).Verifiable();
        mockRegister.Setup(x => x.Register(typeof(IDocumentExecutionListener), typeof(DataLoaderDocumentListener), ServiceLifetime.Singleton, false)).Returns(register).Verifiable();
        mockRegister.Setup(x => x.Register(typeof(DataLoaderDocumentListener), typeof(DataLoaderDocumentListener), ServiceLifetime.Singleton, false)).Returns(register).Verifiable();
        var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        mockServiceProvider.Setup(x => x.GetService(typeof(DataLoaderDocumentListener))).Returns(instance).Verifiable();
        mockRegister.Setup(x => x.Register(typeof(IConfigureExecution), It.IsAny<object>(), false))
            .Returns<Type, IConfigureExecution, bool>((_, action, _) =>
            {
                var options = new ExecutionOptions()
                {
                    RequestServices = mockServiceProvider.Object
                };
                action.ExecuteAsync(options, _ => Task.FromResult<ExecutionResult>(null!)).Wait();
                options.Listeners.ShouldContain(instance);
                return register;
            }).Verifiable();
        builder.AddDataLoader();
        mockServiceProvider.Verify();
        mockRegister.Verify();
    }
}
