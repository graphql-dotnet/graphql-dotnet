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
