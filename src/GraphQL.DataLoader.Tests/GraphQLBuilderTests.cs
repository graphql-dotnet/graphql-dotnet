using GraphQL.DI;
using GraphQL.Execution;
using Moq;

namespace GraphQL.DataLoader.Tests;

public class GraphQLBuilderTests
{
    [Fact]
    public async Task AddDataLoader()
    {
        var instance = new DataLoaderDocumentListener(new DataLoaderContextAccessor());
        var mockRegister = new Mock<IServiceRegister>(MockBehavior.Strict);
        var mockBuilder = new Mock<IGraphQLBuilder>(MockBehavior.Strict);
        var register = mockRegister.Object;
        var builder = mockBuilder.Object;
        mockBuilder.Setup(x => x.Services).Returns(register).Verifiable();
        mockRegister.Setup(x => x.Register(typeof(IDataLoaderContextAccessor), typeof(DataLoaderContextAccessor), ServiceLifetime.Singleton, false)).Returns(register).Verifiable();
        mockRegister.Setup(x => x.TryRegister(typeof(IDocumentExecutionListener), typeof(DataLoaderDocumentListener), ServiceLifetime.Singleton, RegistrationCompareMode.ServiceTypeAndImplementationType)).Returns(register).Verifiable();
        mockRegister.Setup(x => x.Register(typeof(DataLoaderDocumentListener), typeof(DataLoaderDocumentListener), ServiceLifetime.Singleton, false)).Returns(register).Verifiable();
        var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        mockServiceProvider.Setup(x => x.GetService(typeof(DataLoaderDocumentListener))).Returns(instance).Verifiable();
        var actions = new List<IConfigureExecution>();
        mockRegister.Setup(x => x.TryRegister(typeof(IConfigureExecution), It.IsAny<Type>(), ServiceLifetime.Singleton, RegistrationCompareMode.ServiceTypeAndImplementationType))
            .Returns<Type, Type, ServiceLifetime, RegistrationCompareMode>((_, type, _, _) =>
            {
                var action = (IConfigureExecution)Activator.CreateInstance(type)!;
                actions.Add(action);
                return register;
            }).Verifiable();
        builder.AddDataLoader();

        var options = new ExecutionOptions()
        {
            RequestServices = mockServiceProvider.Object
        };
        foreach (var action in actions)
        {
            await action.ExecuteAsync(options, _ => Task.FromResult<ExecutionResult>(null!));
        }
        options.Listeners.ShouldContain(instance);

        mockServiceProvider.Verify();
        mockRegister.Verify();
    }
}
