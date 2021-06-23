using System;
using GraphQL.DI;
using GraphQL.Execution;
using Moq;
using Shouldly;
using Xunit;

namespace GraphQL.DataLoader.Tests
{
    public class GraphQLBuilderTests
    {
        [Fact]
        public void AddDataLoader()
        {
            var instance = new DataLoaderDocumentListener(new DataLoaderContextAccessor());
            var mockBuilder = new Mock<IGraphQLBuilder>(MockBehavior.Strict);
            var builder = mockBuilder.Object;
            mockBuilder.Setup(x => x.Register(typeof(IDataLoaderContextAccessor), typeof(DataLoaderContextAccessor), ServiceLifetime.Singleton)).Returns(builder).Verifiable();
            mockBuilder.Setup(x => x.Register(typeof(IDocumentExecutionListener), typeof(DataLoaderDocumentListener), ServiceLifetime.Singleton)).Returns(builder).Verifiable();
            mockBuilder.Setup(x => x.Register(typeof(DataLoaderDocumentListener), typeof(DataLoaderDocumentListener), ServiceLifetime.Singleton)).Returns(builder).Verifiable();
            var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
            mockServiceProvider.Setup(x => x.GetService(typeof(DataLoaderDocumentListener))).Returns(instance).Verifiable();
            mockBuilder.Setup(x => x.Register(typeof(Action<ExecutionOptions>), It.IsAny<Func<IServiceProvider, object>>(), ServiceLifetime.Singleton))
                .Returns<Type, Func<IServiceProvider, object>, ServiceLifetime>((_, func, serviceLifetime) =>
                {
                    var action = (Action<ExecutionOptions>)func(mockServiceProvider.Object);
                    var options = new ExecutionOptions()
                    {
                        RequestServices = mockServiceProvider.Object
                    };
                    action(options);
                    options.Listeners.ShouldContain(instance);
                    return builder;
                }).Verifiable();
            builder.AddDataLoader();
            mockServiceProvider.Verify();
            mockBuilder.Verify();
        }
    }
}
