using System;
using GraphQL.Caching;
using GraphQL.DI;
using GraphQL.Execution;
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
            mock.Setup(b => b.TryRegister(ServiceLifetime.Transient, It.IsAny<Func<IServiceProvider, IDocumentWriter>>()))
                .Returns<ServiceLifetime, Func<IServiceProvider, IDocumentWriter>>((serviceLifetime, func) =>
                {
                    Should.Throw<InvalidOperationException>(() => func(null));
                    return builder;
                }).Verifiable();
            mock.Setup(b => b.TryRegister(typeof(IDocumentExecuter), typeof(DocumentExecuter), ServiceLifetime.Singleton)).Returns(builder).Verifiable();
            mock.Setup(b => b.TryRegister(typeof(IDocumentBuilder), typeof(GraphQLDocumentBuilder), ServiceLifetime.Singleton)).Returns(builder).Verifiable();
            mock.Setup(b => b.TryRegister(typeof(IDocumentValidator), typeof(DocumentValidator), ServiceLifetime.Singleton)).Returns(builder).Verifiable();
            mock.Setup(b => b.TryRegister(typeof(IComplexityAnalyzer), typeof(ComplexityAnalyzer), ServiceLifetime.Singleton)).Returns(builder).Verifiable();
            mock.Setup(b => b.TryRegister(ServiceLifetime.Singleton, It.IsAny<Func<IServiceProvider, IDocumentCache>>()))
                .Returns<ServiceLifetime, Func<IServiceProvider, IDocumentCache>>((serviceLifetime, func) =>
                {
                    func(null).ShouldBe(DefaultDocumentCache.Instance);
                    return builder;
                }).Verifiable();
            mock.Setup(b => b.TryRegister(typeof(IErrorInfoProvider), typeof(ErrorInfoProvider), ServiceLifetime.Singleton)).Returns(builder).Verifiable();
            mock.Setup(b => b.Register(ServiceLifetime.Singleton, It.IsAny<Func<IServiceProvider, Action<ExecutionOptions>>>()))
                .Returns<ServiceLifetime, Func<IServiceProvider, Action<ExecutionOptions>>>((ServiceLifetime, func) =>
                {
                    var action = func(null);
                    //test null requestservices
                    Should.Throw<InvalidOperationException>(() => action(new ExecutionOptions()));
                    //test null complexityconfiguration
                    var opts = new ExecutionOptions();
                    var mockSp = new Mock<IServiceProvider>(MockBehavior.Strict);
                    mockSp.Setup(s => s.GetService(typeof(ComplexityConfiguration))).Returns(null).Verifiable();
                    opts.RequestServices = mockSp.Object;
                    var tempComplexityConfiguration = new ComplexityConfiguration();
                    opts.ComplexityConfiguration = tempComplexityConfiguration;
                    action(opts);
                    mockSp.Verify();
                    opts.ComplexityConfiguration.ShouldBe(tempComplexityConfiguration);
                    //test value in complexityconfiguration
                    opts = new ExecutionOptions();
                    tempComplexityConfiguration = new ComplexityConfiguration();
                    mockSp = new Mock<IServiceProvider>(MockBehavior.Strict);
                    mockSp.Setup(s => s.GetService(typeof(ComplexityConfiguration))).Returns(tempComplexityConfiguration).Verifiable();
                    opts.RequestServices = mockSp.Object;
                    action(opts);
                    mockSp.Verify();
                    opts.ComplexityConfiguration.ShouldBe(tempComplexityConfiguration);

                    return builder;
                }).Verifiable();
            mock.Setup(b => b.Configure((Action<ComplexityConfiguration, IServiceProvider>)null)).Returns(builder).Verifiable();
            mock.Setup(b => b.Configure((Action<ErrorInfoProviderOptions, IServiceProvider>)null)).Returns(builder).Verifiable();

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

            public override IGraphQLBuilder Register<TService>(ServiceLifetime serviceLifetime, Func<IServiceProvider, TService> implementationFactory)
                => MockBuilder.Object.Register(serviceLifetime, implementationFactory);

            public override IGraphQLBuilder Register(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime)
                => MockBuilder.Object.Register(serviceType, implementationType, serviceLifetime);

            public override IGraphQLBuilder TryRegister<TService>(ServiceLifetime serviceLifetime, Func<IServiceProvider, TService> implementationFactory)
                => MockBuilder.Object.TryRegister(serviceLifetime, implementationFactory);

            public override IGraphQLBuilder TryRegister(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime)
                => MockBuilder.Object.TryRegister(serviceType, implementationType, serviceLifetime);
        }
    }
}
