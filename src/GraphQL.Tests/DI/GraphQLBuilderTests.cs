using System;
using GraphQL.DI;
using GraphQL.Types;
using Moq;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.DI
{
    public class GraphQLBuilderExtensionTests
    {
        private readonly Mock<IGraphQLBuilder> _builderMock = new Mock<IGraphQLBuilder>(MockBehavior.Strict);
        private IGraphQLBuilder _builder => _builderMock.Object;

        private void Verify()
        {
            _builderMock.Verify();
            _builderMock.VerifyNoOtherCalls();
        }

        private void MockSetupRegister<TService, TImplementation>(ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        {
            _builderMock.Setup(b => b.Register(typeof(TService), typeof(TImplementation), serviceLifetime)).Returns((IGraphQLBuilder)null).Verifiable();
        }

        private void MockSetupRegister<TService>(TService instance, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TService : class
        {
            _builderMock.Setup(b => b.Register(serviceLifetime, It.IsAny<Func<IServiceProvider, TService>>())).Returns<ServiceLifetime, Func<IServiceProvider, TService>>((_, factory) =>
            {
                factory(null).ShouldBe(instance);
                return null;
            }).Verifiable();
        }

        private Func<IServiceProvider, TService> MockSetupRegister<TService>(ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TService : class
        {
            Func<IServiceProvider, TService> factory = _ => null;
            _builderMock.Setup(b => b.Register(serviceLifetime, factory)).Returns((IGraphQLBuilder)null).Verifiable();
            return factory;
        }

        #region - Overloads for Register, TryRegister, ConfigureDefaults and Configure -
        [Fact]
        public void Register()
        {
            _builderMock.Setup(x => x.Register(typeof(Class1), typeof(Class1), ServiceLifetime.Transient)).Returns((IGraphQLBuilder)null).Verifiable();
            _builder.Register<Class1>(ServiceLifetime.Transient);
            Verify();
        }

        [Fact]
        public void RegisterImplementation()
        {
            _builderMock.Setup(x => x.Register(typeof(Interface1), typeof(Class1), ServiceLifetime.Transient)).Returns((IGraphQLBuilder)null).Verifiable();
            _builder.Register<Interface1, Class1>(ServiceLifetime.Transient);
            Verify();
        }

        [Fact]
        public void TryRegister()
        {
            _builderMock.Setup(x => x.TryRegister(typeof(Class1), typeof(Class1), ServiceLifetime.Transient)).Returns((IGraphQLBuilder)null).Verifiable();
            _builder.TryRegister<Class1>(ServiceLifetime.Transient);
            Verify();
        }

        [Fact]
        public void TryRegisterImplementation()
        {
            _builderMock.Setup(x => x.TryRegister(typeof(Interface1), typeof(Class1), ServiceLifetime.Transient)).Returns((IGraphQLBuilder)null).Verifiable();
            _builder.TryRegister<Interface1, Class1>(ServiceLifetime.Transient);
            Verify();
        }

        [Fact]
        public void Configure()
        {
            _builderMock.Setup(x => x.Configure(It.IsAny<Action<Class1, IServiceProvider>>())).Returns<Action<Class1, IServiceProvider>>(a => {
                var c = new Class1();
                a(c, null);
                c.Value.ShouldBe(1);
                return null;
            }).Verifiable();
            _builder.Configure<Class1>(x => x.Value = 1);
        }

        [Fact]
        public void ConfigureNull()
        {
            _builderMock.Setup(x => x.Configure<Class1>(null)).Returns((IGraphQLBuilder)null).Verifiable();
            Action<Class1> arg = null;
            _builder.Configure(arg);
        }

        [Fact]
        public void ConfigureDefaults()
        {
            _builderMock.Setup(x => x.ConfigureDefaults(It.IsAny<Action<Class1, IServiceProvider>>())).Returns<Action<Class1, IServiceProvider>>(a => {
                var c = new Class1();
                a(c, null);
                c.Value.ShouldBe(1);
                return null;
            }).Verifiable();
            _builder.ConfigureDefaults<Class1>(x => x.Value = 1);
        }

        [Fact]
        public void ConfigureDefaultsNull()
        {
            _builderMock.Setup(x => x.ConfigureDefaults<Class1>(null)).Returns((IGraphQLBuilder)null).Verifiable();
            Action<Class1> arg = null;
            _builder.ConfigureDefaults(arg);
        }
        #endregion

        #region - AddSchema -
        [Fact]
        public void AddSchema()
        {
            _builderMock.Setup(b => b.Register(typeof(TestSchema), typeof(TestSchema), ServiceLifetime.Singleton)).Returns((IGraphQLBuilder)null).Verifiable();
            _builderMock.Setup(b => b.TryRegister(typeof(ISchema), typeof(TestSchema), ServiceLifetime.Singleton)).Returns((IGraphQLBuilder)null).Verifiable();
            _builder.AddSchema<TestSchema>();
            Verify();
        }

        [Fact]
        public void AddSchema_Transient()
        {
            Should.Throw<InvalidOperationException>(() => _builder.AddSchema<TestSchema>(ServiceLifetime.Transient));
            Should.Throw<InvalidOperationException>(() => _builder.AddSchema<TestSchema>(_ => null, ServiceLifetime.Transient));
        }

        [Fact]
        public void AddSchema_Scoped()
        {
            _builderMock.Setup(b => b.Register(typeof(TestSchema), typeof(TestSchema), ServiceLifetime.Scoped)).Returns((IGraphQLBuilder)null).Verifiable();
            _builderMock.Setup(b => b.TryRegister(typeof(ISchema), typeof(TestSchema), ServiceLifetime.Scoped)).Returns((IGraphQLBuilder)null).Verifiable();
            _builder.AddSchema<TestSchema>(ServiceLifetime.Scoped);
            Verify();
        }

        [Fact]
        public void AddSchema_Factory()
        {
            Func<IServiceProvider, TestSchema> factory = _ => null;
            _builderMock.Setup(b => b.Register(ServiceLifetime.Singleton, factory)).Returns((IGraphQLBuilder)null).Verifiable();
            _builderMock.Setup(b => b.TryRegister<ISchema>(ServiceLifetime.Singleton, factory)).Returns((IGraphQLBuilder)null).Verifiable();
            _builder.AddSchema(factory);
            Verify();
        }

        [Fact]
        public void AddSchema_Instance()
        {
            var schema = new TestSchema();
            _builderMock.Setup(b => b.Register(ServiceLifetime.Singleton, It.IsAny<Func<IServiceProvider, TestSchema>>())).Returns<ServiceLifetime, Func<IServiceProvider, TestSchema>>((serviceLifetime, factory) => {
                var schema2 = factory(null);
                schema2.ShouldBe(schema);
                return null;
            }).Verifiable();
            _builderMock.Setup(b => b.TryRegister<ISchema>(ServiceLifetime.Singleton, It.IsAny<Func<IServiceProvider, TestSchema>>())).Returns<ServiceLifetime, Func<IServiceProvider, ISchema>>((serviceLifetime, factory) => {
                var schema2 = factory(null);
                schema2.ShouldBe(schema);
                return null;
            }).Verifiable();
            _builder.AddSchema(schema);
            Verify();
        }

        [Fact]
        public void AddSchema_InstanceNull()
        {
            Should.Throw<ArgumentNullException>(() => _builder.AddSchema((TestSchema)null));
        }

        [Fact]
        public void AddSchema_FactoryNull()
        {
            Should.Throw<ArgumentNullException>(() => _builder.AddSchema((Func<IServiceProvider, TestSchema>)null));
        }
        #endregion

        #region - AddDocumentExecuter -
        [Fact]
        public void AddDocumentExecuter()
        {
            MockSetupRegister<IDocumentExecuter, TestDocumentExecuter>();
            _builder.AddDocumentExecuter<TestDocumentExecuter>();
            Verify();
        }

        [Fact]
        public void AddDocumentExecuter_Instance()
        {
            var instance = new TestDocumentExecuter();
            MockSetupRegister<IDocumentExecuter>(instance);
            _builder.AddDocumentExecuter(instance);
            Verify();
        }

        [Fact]
        public void AddDocumentExecuter_Factory()
        {
            var factory = MockSetupRegister<IDocumentExecuter>();
            _builder.AddDocumentExecuter(factory);
            Verify();
        }

        [Fact]
        public void AddDocumentExecuter_Null()
        {
            Should.Throw<ArgumentNullException>(() => _builder.AddDocumentExecuter((TestDocumentExecuter)null));
            Should.Throw<ArgumentNullException>(() => _builder.AddDocumentExecuter((Func<IServiceProvider, TestDocumentExecuter>)null));
        }
        #endregion

        private class Class1 : Interface1
        {
            public int Value { get; set; }
        }

        private interface Interface1
        {
        }

        private class TestSchema : Schema
        {
        }

        private class TestDocumentExecuter : DocumentExecuter
        {
        }
    }
}
