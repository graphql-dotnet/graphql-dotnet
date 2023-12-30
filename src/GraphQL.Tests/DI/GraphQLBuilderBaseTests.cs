using GraphQL.DI;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Types.Relay;
using GraphQL.Validation;
using Moq;

namespace GraphQL.Tests.DI;

public class GraphQLBuilderBaseTests
{
    [Fact]
    public async Task Initialize()
    {
        var builder = new TestBuilder();
        var mock = builder.ServiceRegister.MockBuilder;
        mock.Setup(b => b.TryRegister(typeof(IGraphQLSerializer), It.IsAny<Func<IServiceProvider, object>>(), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType))
            .Returns<Type, Func<IServiceProvider, object>, ServiceLifetime, RegistrationCompareMode>((_, func, serviceLifetime, _) =>
            {
                Should.Throw<InvalidOperationException>(() => func(null!));
                return builder.ServiceRegister;
            }).Verifiable();
        mock.Setup(b => b.TryRegister(typeof(IGraphQLTextSerializer), It.IsAny<Func<IServiceProvider, object>>(), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType))
            .Returns<Type, Func<IServiceProvider, object>, ServiceLifetime, RegistrationCompareMode>((_, func, serviceLifetime, _) =>
            {
                Should.Throw<InvalidOperationException>(() => func(null!));
                return builder.ServiceRegister;
            }).Verifiable();
        mock.Setup(b => b.TryRegister(typeof(IDocumentExecuter), typeof(DocumentExecuter), ServiceLifetime.Singleton, RegistrationCompareMode.ServiceType)).Returns(builder.ServiceRegister).Verifiable();
        mock.Setup(b => b.TryRegister(typeof(IDocumentExecuter<>), typeof(DocumentExecuter<>), ServiceLifetime.Singleton, RegistrationCompareMode.ServiceType)).Returns(builder.ServiceRegister).Verifiable();
        mock.Setup(b => b.TryRegister(typeof(IDocumentBuilder), typeof(GraphQLDocumentBuilder), ServiceLifetime.Singleton, RegistrationCompareMode.ServiceType)).Returns(builder.ServiceRegister).Verifiable();
        mock.Setup(b => b.TryRegister(typeof(IDocumentValidator), typeof(DocumentValidator), ServiceLifetime.Singleton, RegistrationCompareMode.ServiceType)).Returns(builder.ServiceRegister).Verifiable();
        mock.Setup(b => b.TryRegister(typeof(IErrorInfoProvider), typeof(ErrorInfoProvider), ServiceLifetime.Singleton, RegistrationCompareMode.ServiceType)).Returns(builder.ServiceRegister).Verifiable();
        mock.Setup(b => b.TryRegister(typeof(IExecutionStrategySelector), typeof(DefaultExecutionStrategySelector), ServiceLifetime.Singleton, RegistrationCompareMode.ServiceType)).Returns(builder.ServiceRegister).Verifiable();
        mock.Setup(b => b.Configure((Action<ErrorInfoProviderOptions, IServiceProvider>)null!)).Returns(builder.ServiceRegister).Verifiable();
        var actions = new List<IConfigureExecution>();
        mock.Setup(b => b.Register(typeof(IConfigureExecution), It.IsAny<IConfigureExecution>(), false)).Returns<Type, IConfigureExecution, bool>((_, action, _) =>
        {
            actions.Add(action);
            return builder.ServiceRegister;
        }).Verifiable();

        mock.Setup(b => b.TryRegister(typeof(EnumerationGraphType<>), typeof(EnumerationGraphType<>), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(builder.ServiceRegister).Verifiable();
        mock.Setup(b => b.TryRegister(typeof(ConnectionType<>), typeof(ConnectionType<>), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(builder.ServiceRegister).Verifiable();
        mock.Setup(b => b.TryRegister(typeof(ConnectionType<,>), typeof(ConnectionType<,>), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(builder.ServiceRegister).Verifiable();
        mock.Setup(b => b.TryRegister(typeof(EdgeType<>), typeof(EdgeType<>), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(builder.ServiceRegister).Verifiable();
        mock.Setup(b => b.TryRegister(typeof(PageInfoType), typeof(PageInfoType), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(builder.ServiceRegister).Verifiable();
        mock.Setup(b => b.TryRegister(typeof(InputObjectGraphType<>), typeof(InputObjectGraphType<>), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(builder.ServiceRegister).Verifiable();
        mock.Setup(b => b.TryRegister(typeof(AutoRegisteringInputObjectGraphType<>), typeof(AutoRegisteringInputObjectGraphType<>), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(builder.ServiceRegister).Verifiable();
        mock.Setup(b => b.TryRegister(typeof(AutoRegisteringObjectGraphType<>), typeof(AutoRegisteringObjectGraphType<>), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(builder.ServiceRegister).Verifiable();
        mock.Setup(b => b.TryRegister(typeof(AutoRegisteringInterfaceGraphType<>), typeof(AutoRegisteringInterfaceGraphType<>), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(builder.ServiceRegister).Verifiable();

        builder.CallInitialize();

        foreach (var action in actions)
        {
            var schema = Mock.Of<ISchema>(MockBehavior.Strict);

            //verify no action if schema is set
            await action.ExecuteAsync(new ExecutionOptions { Schema = schema, RequestServices = Mock.Of<IServiceProvider>(MockBehavior.Strict) }, _ => Task.FromResult<ExecutionResult>(null!));

            //verify schema is pulled from service provider if schema is not set
            var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
            mockServiceProvider.Setup(s => s.GetService(typeof(ISchema))).Returns(schema).Verifiable();
            var opts = new ExecutionOptions()
            {
                RequestServices = mockServiceProvider.Object,
            };
            await action.ExecuteAsync(opts, _ => Task.FromResult<ExecutionResult>(null!));
            opts.Schema.ShouldBe(schema);
            mockServiceProvider.Verify();
        }

        mock.Verify();
        mock.VerifyNoOtherCalls();
    }

    private class TestServiceRegister : IServiceRegister
    {
        public readonly Mock<IServiceRegister> MockBuilder = new(MockBehavior.Strict);

        public IServiceRegister Configure<TOptions>(Action<TOptions, IServiceProvider>? action = null)
            where TOptions : class, new()
            => MockBuilder.Object.Configure(action);

        public IServiceRegister Register(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime serviceLifetime, bool replace = false)
            => MockBuilder.Object.Register(serviceType, implementationFactory, serviceLifetime, replace);

        public IServiceRegister Register(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime, bool replace = false)
            => MockBuilder.Object.Register(serviceType, implementationType, serviceLifetime, replace);

        public IServiceRegister Register(Type serviceType, object implementationInstance, bool replace = false)
            => MockBuilder.Object.Register(serviceType, implementationInstance, replace);

        public IServiceRegister TryRegister(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime serviceLifetime, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
            => MockBuilder.Object.TryRegister(serviceType, implementationFactory, serviceLifetime, mode);

        public IServiceRegister TryRegister(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
            => MockBuilder.Object.TryRegister(serviceType, implementationType, serviceLifetime, mode);

        public IServiceRegister TryRegister(Type serviceType, object implementationInstance, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
            => MockBuilder.Object.TryRegister(serviceType, implementationInstance, mode);
    }

    private class TestBuilder : GraphQLBuilderBase
    {
        public TestServiceRegister ServiceRegister { get; set; } = new TestServiceRegister();

        public void CallInitialize() => RegisterDefaultServices();

        public override IServiceRegister Services => ServiceRegister;
    }
}
