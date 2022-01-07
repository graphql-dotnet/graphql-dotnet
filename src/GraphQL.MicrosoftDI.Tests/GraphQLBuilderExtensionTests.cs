using System;
using System.Linq;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace GraphQL.MicrosoftDI.Tests
{
    public class GraphQLBuilderExtensionTests
    {
        [Fact]
        public void AddGraphQL()
        {
            var services = new ServiceCollection();
            var builder = services.AddGraphQL();
            builder.ShouldBeOfType<GraphQLBuilder>();
            services.BuildServiceProvider().GetService<IDocumentExecuter>().ShouldNotBeNull();
        }

        [Theory]
        [InlineData(DI.ServiceLifetime.Singleton)]
        [InlineData(DI.ServiceLifetime.Scoped)]
        public void AddSelfActivatingSchema(DI.ServiceLifetime serviceLifetime)
        {
            var services = new ServiceCollection();
            services.AddSingleton(Class2.Instance);
            var builder = services.AddGraphQL()
                .AddSelfActivatingSchema<MySchema>(serviceLifetime);
            services.Single(x => x.ServiceType == typeof(MySchema)).Lifetime.ShouldBe(serviceLifetime switch
            {
                DI.ServiceLifetime.Singleton => ServiceLifetime.Singleton,
                DI.ServiceLifetime.Scoped => ServiceLifetime.Scoped,
                _ => throw new ApplicationException()
            });
            services.Single(x => x.ServiceType == typeof(ISchema)).Lifetime.ShouldBe(serviceLifetime switch
            {
                DI.ServiceLifetime.Singleton => ServiceLifetime.Singleton,
                DI.ServiceLifetime.Scoped => ServiceLifetime.Scoped,
                _ => throw new ApplicationException()
            });
            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.GetRequiredService<MySchema>().ShouldBeOfType<MySchema>();
            serviceProvider.GetRequiredService<ISchema>().ShouldBeOfType<MySchema>();
        }

        [Fact]
        public void AddSelfActivatingSchema_Transient()
        {
            var services = new ServiceCollection();
            var builder = services.AddGraphQL();
            Should.Throw<InvalidOperationException>(() => builder.AddSelfActivatingSchema<MySchema>(DI.ServiceLifetime.Transient));
        }

        private class MySchema : Schema
        {
            public MySchema(IServiceProvider serviceProvider) : base(serviceProvider)
            {
                serviceProvider.ShouldBeOfType<SelfActivatingServiceProvider>();
                //test pulling a registered class
                serviceProvider.GetRequiredService<Class2>().ShouldBe(Class2.Instance);
                //test pulling an unregistered class
                serviceProvider.GetRequiredService<Class1>().ShouldNotBeNull();
            }
        }

        private class Class1
        {
        }

        private class Class2
        {
            public static Class2 Instance = new Class2();

            private Class2()
            {
            }
        }
    }
}
