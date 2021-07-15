using System;
using GraphQL.Caching;
using GraphQL.DI;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Types.Relay;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using Moq;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.DI
{
    public class GraphQLBuilderBaseTests
    {
        [Fact]
        public void Initialize()
        {
            var builder = new TestBuilder();
            var mock = builder.MockBuilder;
            mock.Setup(b => b.TryRegister(typeof(IDocumentWriter), It.IsAny<Func<IServiceProvider, object>>(), ServiceLifetime.Transient))
                .Returns<Type, Func<IServiceProvider, object>, ServiceLifetime>((_, func, serviceLifetime) =>
                {
                    Should.Throw<InvalidOperationException>(() => func(null));
                    return builder;
                }).Verifiable();
            mock.Setup(b => b.TryRegister(typeof(IDocumentExecuter), typeof(DocumentExecuter), ServiceLifetime.Singleton)).Returns(builder).Verifiable();
            mock.Setup(b => b.TryRegister(typeof(IDocumentBuilder), typeof(GraphQLDocumentBuilder), ServiceLifetime.Singleton)).Returns(builder).Verifiable();
            mock.Setup(b => b.TryRegister(typeof(IDocumentValidator), typeof(DocumentValidator), ServiceLifetime.Singleton)).Returns(builder).Verifiable();
            mock.Setup(b => b.TryRegister(typeof(IComplexityAnalyzer), typeof(ComplexityAnalyzer), ServiceLifetime.Singleton)).Returns(builder).Verifiable();
            mock.Setup(b => b.TryRegister(typeof(IDocumentCache), DefaultDocumentCache.Instance)).Returns(builder).Verifiable();
            mock.Setup(b => b.TryRegister(typeof(IErrorInfoProvider), typeof(ErrorInfoProvider), ServiceLifetime.Singleton)).Returns(builder).Verifiable();
            mock.Setup(b => b.Configure((Action<ErrorInfoProviderOptions, IServiceProvider>)null)).Returns(builder).Verifiable();
            mock.Setup(b => b.Register(typeof(IConfigureExecution), It.IsAny<IConfigureExecution>())).Returns<Type, IConfigureExecution>((_, action) =>
            {
                var schema = Mock.Of<ISchema>(MockBehavior.Strict);

                //verify no action if schema is set
                action.Configure(new ExecutionOptions { Schema = schema, RequestServices = Mock.Of<IServiceProvider>(MockBehavior.Strict) });

                //verify schema is pulled from service provider if schema is not set
                var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
                mockServiceProvider.Setup(s => s.GetService(typeof(ISchema))).Returns(schema).Verifiable();
                var opts = new ExecutionOptions()
                {
                    RequestServices = mockServiceProvider.Object,
                };
                action.Configure(opts);
                opts.Schema.ShouldBe(schema);
                mockServiceProvider.Verify();

                return builder;
            }).Verifiable();

            mock.Setup(b => b.TryRegister(typeof(EnumerationGraphType<>), typeof(EnumerationGraphType<>), ServiceLifetime.Transient)).Returns(builder).Verifiable();
            mock.Setup(b => b.TryRegister(typeof(ConnectionType<>), typeof(ConnectionType<>), ServiceLifetime.Transient)).Returns(builder).Verifiable();
            mock.Setup(b => b.TryRegister(typeof(ConnectionType<,>), typeof(ConnectionType<,>), ServiceLifetime.Transient)).Returns(builder).Verifiable();
            mock.Setup(b => b.TryRegister(typeof(EdgeType<>), typeof(EdgeType<>), ServiceLifetime.Transient)).Returns(builder).Verifiable();
            mock.Setup(b => b.TryRegister(typeof(PageInfoType), typeof(PageInfoType), ServiceLifetime.Transient)).Returns(builder).Verifiable();
            mock.Setup(b => b.TryRegister(typeof(InputObjectGraphType<>), typeof(InputObjectGraphType<>), ServiceLifetime.Transient)).Returns(builder).Verifiable();
            mock.Setup(b => b.TryRegister(typeof(AutoRegisteringInputObjectGraphType<>), typeof(AutoRegisteringInputObjectGraphType<>), ServiceLifetime.Transient)).Returns(builder).Verifiable();
            mock.Setup(b => b.TryRegister(typeof(AutoRegisteringObjectGraphType<>), typeof(AutoRegisteringObjectGraphType<>), ServiceLifetime.Transient)).Returns(builder).Verifiable();

            builder.CallInitialize();
            mock.Verify();
            mock.VerifyNoOtherCalls();
        }

        private class TestBuilder : GraphQLBuilderBase
        {
            public readonly Mock<IGraphQLBuilder> MockBuilder = new Mock<IGraphQLBuilder>(MockBehavior.Strict);

            public void CallInitialize()
                => Initialize();

            public override IGraphQLBuilder Configure<TOptions>(Action<TOptions, IServiceProvider> action = null)
                => MockBuilder.Object.Configure(action);

            public override IGraphQLBuilder Register(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime serviceLifetime)
                => MockBuilder.Object.Register(serviceType, implementationFactory, serviceLifetime);

            public override IGraphQLBuilder Register(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime)
                => MockBuilder.Object.Register(serviceType, implementationType, serviceLifetime);

            public override IGraphQLBuilder Register(Type serviceType, object implementationInstance)
                => MockBuilder.Object.Register(serviceType, implementationInstance);

            public override IGraphQLBuilder TryRegister(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime serviceLifetime)
                => MockBuilder.Object.TryRegister(serviceType, implementationFactory, serviceLifetime);

            public override IGraphQLBuilder TryRegister(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime)
                => MockBuilder.Object.TryRegister(serviceType, implementationType, serviceLifetime);

            public override IGraphQLBuilder TryRegister(Type serviceType, object implementationInstance)
                => MockBuilder.Object.TryRegister(serviceType, implementationInstance);
        }
    }
}
