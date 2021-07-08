using System;
using System.Threading.Tasks;
using GraphQL.Caching;
using GraphQL.DI;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.NewtonsoftJson;
using GraphQL.SystemReactive;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using GraphQL.Types.Relay;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
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
            _builderMock.Setup(b => b.Register(typeof(TService), typeof(TImplementation), serviceLifetime)).Returns(_builder).Verifiable();
        }

        private void MockSetupRegister<TService>(TService instance)
            where TService : class
        {
            _builderMock.Setup(b => b.Register(typeof(TService), instance)).Returns(_builder).Verifiable();
        }

        private Func<IServiceProvider, TService> MockSetupRegister<TService>(ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TService : class
        {
            Func<IServiceProvider, TService> factory = _ => null;
            _builderMock.Setup(b => b.Register(typeof(TService), factory, serviceLifetime)).Returns(_builder).Verifiable();
            return factory;
        }

        private Action<TOptions> MockSetupConfigure1<TOptions>()
            where TOptions : class, new()
        {
            int ran = 0;
            Action<TOptions> action = options => ran++;
            _builderMock.Setup(b => b.Configure(It.IsAny<Action<TOptions, IServiceProvider>>())).Returns<Action<TOptions, IServiceProvider>>(action2 =>
            {
                ran.ShouldBe(0);
                action2(null, null);
                ran.ShouldBe(1);
                return _builder;
            }).Verifiable();
            return action;
        }

        private Action<TOptions, IServiceProvider> MockSetupConfigure2<TOptions>()
            where TOptions : class, new()
        {
            Action<TOptions, IServiceProvider> action = (opts, _) => { };
            _builderMock.Setup(b => b.Configure(action)).Returns(_builder).Verifiable();
            return action;
        }

        private void MockSetupConfigureNull<TOptions>()
            where TOptions : class, new()
        {
            _builderMock.Setup(b => b.Configure((Action<TOptions, IServiceProvider>)null)).Returns(_builder).Verifiable();
        }

        private Func<ExecutionOptions> MockSetupConfigureExecution(IServiceProvider serviceProvider = null)
        {
            Action<ExecutionOptions> actions = _ => { };
            _builderMock.Setup(b => b.Register(typeof(IConfigureExecution), It.IsAny<object>()))
                .Returns<Type, IConfigureExecution>((_, action) =>
                {
                    var actions2 = actions;
                    actions = opts =>
                    {
                        actions2(opts);
                        action.Configure(opts);
                    };
                    return _builder;
                }).Verifiable();
            return () =>
            {
                var opts = new ExecutionOptions()
                {
                    RequestServices = serviceProvider,
                };
                actions(opts);
                return opts;
            };
        }

        private Action MockSetupConfigureSchema(ISchema schema, IServiceProvider serviceProvider = null)
        {
            Action<ISchema, IServiceProvider> actions = (_, _) => { };
            _builderMock.Setup(b => b.Register(typeof(IConfigureSchema), It.IsAny<object>()))
                .Returns<Type, IConfigureSchema>((_, action) =>
                {
                    var actions2 = actions;
                    actions = (opts, services) =>
                    {
                        actions2(opts, services);
                        action.Configure(opts, services);
                    };
                    return _builder;
                }).Verifiable();
            return () => actions(schema, serviceProvider);
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
        public void Register_Implementation()
        {
            _builderMock.Setup(x => x.Register(typeof(Interface1), typeof(Class1), ServiceLifetime.Transient)).Returns((IGraphQLBuilder)null).Verifiable();
            _builder.Register<Interface1, Class1>(ServiceLifetime.Transient);
            Verify();
        }

        [Fact]
        public void Register_Instance()
        {
            var instance = new Class1();
            _builderMock.Setup(x => x.Register(typeof(Interface1), instance)).Returns(_builder).Verifiable();
            _builder.Register<Interface1>(instance);
            Verify();
        }

        [Fact]
        public void Register_Factory()
        {
            var factory = MockSetupRegister<Class1>(ServiceLifetime.Transient);
            _builder.Register(factory, ServiceLifetime.Transient);
            Verify();
        }

        [Fact]
        public void Register_Null()
        {
            Should.Throw<ArgumentNullException>(() => _builder.Register<Class1>(implementationInstance: null));
            Should.Throw<ArgumentNullException>(() => _builder.Register<Class1>(implementationFactory: null, ServiceLifetime.Singleton));
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
        public void TryRegister_Instance()
        {
            var instance = new Class1();
            _builderMock.Setup(x => x.TryRegister(typeof(Interface1), instance)).Returns(_builder).Verifiable();
            _builder.TryRegister<Interface1>(instance);
            Verify();
        }

        [Fact]
        public void TryRegister_Factory()
        {
            Func<IServiceProvider, Class1> factory = _ => null;
            _builderMock.Setup(b => b.TryRegister(typeof(Class1), factory, ServiceLifetime.Transient)).Returns(_builder).Verifiable();
            _builder.TryRegister(factory, ServiceLifetime.Transient);
            Verify();
        }

        [Fact]
        public void TryRegister_Null()
        {
            Should.Throw<ArgumentNullException>(() => _builder.TryRegister<Class1>(implementationInstance: null));
            Should.Throw<ArgumentNullException>(() => _builder.TryRegister<Class1>(implementationFactory: null, ServiceLifetime.Singleton));
        }

        [Fact]
        public void Configure()
        {
            _builderMock.Setup(x => x.Configure(It.IsAny<Action<Class1, IServiceProvider>>())).Returns<Action<Class1, IServiceProvider>>(a =>
            {
                var c = new Class1();
                a(c, null);
                c.Value.ShouldBe(1);
                return null;
            }).Verifiable();
            _builder.Configure<Class1>(x => x.Value = 1);
            Verify();
        }

        [Fact]
        public void ConfigureNull()
        {
            _builderMock.Setup(x => x.Configure<Class1>(null)).Returns((IGraphQLBuilder)null).Verifiable();
            Action<Class1> arg = null;
            _builder.Configure(arg);
            Verify();
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
            _builderMock.Setup(b => b.Register(typeof(TestSchema), factory, ServiceLifetime.Singleton)).Returns((IGraphQLBuilder)null).Verifiable();
            _builderMock.Setup(b => b.TryRegister(typeof(ISchema), factory, ServiceLifetime.Singleton)).Returns((IGraphQLBuilder)null).Verifiable();
            _builder.AddSchema(factory);
            Verify();
        }

        [Fact]
        public void AddSchema_Instance()
        {
            var schema = new TestSchema();
            _builderMock.Setup(b => b.Register(typeof(TestSchema), It.IsAny<Func<IServiceProvider, object>>(), ServiceLifetime.Singleton))
                .Returns<Type, Func<IServiceProvider, object>, ServiceLifetime>((_, factory, _) =>
                {
                    factory(null).ShouldBe(schema);
                    return null;
                }).Verifiable();
            _builderMock.Setup(b => b.TryRegister(typeof(ISchema), It.IsAny<Func<IServiceProvider, object>>(), ServiceLifetime.Singleton))
                .Returns<Type, Func<IServiceProvider, object>, ServiceLifetime>((_, factory, _) =>
                {
                    factory(null).ShouldBe(schema);
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

        #region - AddComplexityAnalyzer -
        private Action<ComplexityConfiguration> MockSetupComplexityConfiguration1()
        {
            bool ran = false;
            ExecutionOptions opts = null;
            Action<ComplexityConfiguration> configureAction = cc =>
            {
                cc.ShouldNotBeNull();
                cc.ShouldBe(opts.ComplexityConfiguration);
                ran = true;
            };
            _builderMock.Setup(x => x.Register(typeof(IConfigureExecution), It.IsAny<object>()))
                .Returns<Type, IConfigureExecution>((_, action) =>
                {
                    //test with no complexity configuration
                    ran = false;
                    opts = new ExecutionOptions();
                    action.Configure(opts);
                    ran.ShouldBeTrue();

                    //test with existing complexity configuration
                    ran = false;
                    var cc2 = new ComplexityConfiguration();
                    opts = new ExecutionOptions()
                    {
                        ComplexityConfiguration = cc2,
                    };
                    action.Configure(opts);
                    ran.ShouldBeTrue();
                    opts.ComplexityConfiguration.ShouldBe(cc2);

                    return _builder;
                }).Verifiable();

            return configureAction;
        }

        private Action<ComplexityConfiguration, IServiceProvider> MockSetupComplexityConfiguration2()
        {
            bool ran = false;
            ExecutionOptions opts = null;
            Action<ComplexityConfiguration, IServiceProvider> configureAction = (cc, sp) =>
            {
                sp.ShouldBe(opts.RequestServices);
                cc.ShouldNotBeNull();
                cc.ShouldBe(opts.ComplexityConfiguration);
                ran = true;
            };
            _builderMock.Setup(x => x.Register(typeof(IConfigureExecution), It.IsAny<object>()))
                .Returns<Type, IConfigureExecution>((_, action) =>
                {
                    //test with no complexity configuration
                    ran = false;
                    opts = new ExecutionOptions()
                    {
                        RequestServices = new Mock<IServiceProvider>(MockBehavior.Strict).Object,
                    };
                    action.Configure(opts);
                    ran.ShouldBeTrue();

                    //test with existing complexity configuration
                    ran = false;
                    var cc2 = new ComplexityConfiguration();
                    opts = new ExecutionOptions()
                    {
                        RequestServices = new Mock<IServiceProvider>(MockBehavior.Strict).Object,
                        ComplexityConfiguration = cc2,
                    };
                    action.Configure(opts);
                    ran.ShouldBeTrue();
                    opts.ComplexityConfiguration.ShouldBe(cc2);

                    return _builder;
                }).Verifiable();

            return configureAction;
        }

        private void MockSetupComplexityConfigurationNull()
        {
            ExecutionOptions opts = null;
            _builderMock.Setup(x => x.Register(typeof(IConfigureExecution), It.IsAny<object>()))
                .Returns<Type, IConfigureExecution>((_, action) =>
                {
                    //test with no complexity configuration
                    opts = new ExecutionOptions();
                    opts.ComplexityConfiguration.ShouldBeNull();
                    action.Configure(opts);
                    opts.ComplexityConfiguration.ShouldNotBeNull();

                    //test with existing complexity configuration
                    var cc2 = new ComplexityConfiguration();
                    opts = new ExecutionOptions()
                    {
                        ComplexityConfiguration = cc2,
                    };
                    action.Configure(opts);
                    opts.ComplexityConfiguration.ShouldBe(cc2);

                    return _builder;
                }).Verifiable();
        }

        [Fact]
        public void AddComplexityAnalyzer()
        {
            var action = MockSetupComplexityConfiguration1();
            _builder.AddComplexityAnalyzer(action);
            Verify();
        }

        [Fact]
        public void AddComplexityAnalyzer2()
        {
            var action = MockSetupComplexityConfiguration2();
            _builder.AddComplexityAnalyzer(action);
            Verify();
        }

        [Fact]
        public void AddComplexityAnalyzer_Null1()
        {
            MockSetupComplexityConfigurationNull();
            _builder.AddComplexityAnalyzer();
            Verify();
        }

        [Fact]
        public void AddComplexityAnalyzer_Null2()
        {
            MockSetupComplexityConfigurationNull();
            _builder.AddComplexityAnalyzer((Action<ComplexityConfiguration, IServiceProvider>)null);
            Verify();
        }

        [Fact]
        public void AddComplexityAnalyzer_Typed1()
        {
            MockSetupRegister<IComplexityAnalyzer, TestComplexityAnalyzer>();
            var action = MockSetupComplexityConfiguration1();
            _builder.AddComplexityAnalyzer<TestComplexityAnalyzer>(action);
            Verify();
        }

        [Fact]
        public void AddComplexityAnalyzer_Typed2()
        {
            MockSetupRegister<IComplexityAnalyzer, TestComplexityAnalyzer>();
            var action = MockSetupComplexityConfiguration2();
            _builder.AddComplexityAnalyzer<TestComplexityAnalyzer>(action);
            Verify();
        }

        [Fact]
        public void AddComplexityAnalyzer_Typed1_Null()
        {
            MockSetupRegister<IComplexityAnalyzer, TestComplexityAnalyzer>();
            MockSetupComplexityConfigurationNull();
            _builder.AddComplexityAnalyzer<TestComplexityAnalyzer>();
            Verify();
        }

        [Fact]
        public void AddComplexityAnalyzer_Typed2_Null()
        {
            MockSetupRegister<IComplexityAnalyzer, TestComplexityAnalyzer>();
            MockSetupComplexityConfigurationNull();
            _builder.AddComplexityAnalyzer<TestComplexityAnalyzer>((Action<ComplexityConfiguration, IServiceProvider>)null);
            Verify();
        }

        [Fact]
        public void AddComplexityAnalyzer_Instance1()
        {
            var instance = new TestComplexityAnalyzer();
            MockSetupRegister<IComplexityAnalyzer>(instance);
            var action = MockSetupComplexityConfiguration1();
            _builder.AddComplexityAnalyzer(instance, action);
            Verify();
        }

        [Fact]
        public void AddComplexityAnalyzer_Instance2()
        {
            var instance = new TestComplexityAnalyzer();
            MockSetupRegister<IComplexityAnalyzer>(instance);
            var action = MockSetupComplexityConfiguration2();
            _builder.AddComplexityAnalyzer(instance, action);
            Verify();
        }

        [Fact]
        public void AddComplexityAnalyzer_Instance1_Null()
        {
            var instance = new TestComplexityAnalyzer();
            MockSetupRegister<IComplexityAnalyzer>(instance);
            MockSetupComplexityConfigurationNull();
            _builder.AddComplexityAnalyzer(instance);
            Verify();
        }

        [Fact]
        public void AddComplexityAnalyzer_Instance2_Null()
        {
            var instance = new TestComplexityAnalyzer();
            MockSetupRegister<IComplexityAnalyzer>(instance);
            MockSetupComplexityConfigurationNull();
            _builder.AddComplexityAnalyzer(instance, (Action<ComplexityConfiguration, IServiceProvider>)null);
            Verify();
        }

        [Fact]
        public void AddComplexityAnalyzer_InstanceNull()
        {
            Should.Throw<ArgumentNullException>(() => _builder.AddComplexityAnalyzer((IComplexityAnalyzer)null));
            Should.Throw<ArgumentNullException>(() => _builder.AddComplexityAnalyzer((IComplexityAnalyzer)null, (_, _) => { }));
        }

        [Fact]
        public void AddComplexityAnalyzer_Factory1()
        {
            var factory = MockSetupRegister<IComplexityAnalyzer>();
            var action = MockSetupComplexityConfiguration1();
            _builder.AddComplexityAnalyzer(factory, action);
            Verify();
        }

        [Fact]
        public void AddComplexityAnalyzer_Factory2()
        {
            var factory = MockSetupRegister<IComplexityAnalyzer>();
            var action = MockSetupComplexityConfiguration2();
            _builder.AddComplexityAnalyzer(factory, action);
            Verify();
        }

        [Fact]
        public void AddComplexityAnalyzer_Factory1_Null()
        {
            var factory = MockSetupRegister<IComplexityAnalyzer>();
            MockSetupComplexityConfigurationNull();
            _builder.AddComplexityAnalyzer(factory);
            Verify();
        }

        [Fact]
        public void AddComplexityAnalyzer_Factory2_Null()
        {
            var factory = MockSetupRegister<IComplexityAnalyzer>();
            MockSetupComplexityConfigurationNull();
            _builder.AddComplexityAnalyzer(factory, (Action<ComplexityConfiguration, IServiceProvider>)null);
            Verify();
        }

        [Fact]
        public void AddComplexityAnalyzer_FactoryNull()
        {
            Should.Throw<ArgumentNullException>(() => _builder.AddComplexityAnalyzer((TestComplexityAnalyzer)null));
            Should.Throw<ArgumentNullException>(() => _builder.AddComplexityAnalyzer((TestComplexityAnalyzer)null, (_, _) => { }));
            Should.Throw<ArgumentNullException>(() => _builder.AddComplexityAnalyzer((Func<IServiceProvider, IComplexityAnalyzer>)null));
            Should.Throw<ArgumentNullException>(() => _builder.AddComplexityAnalyzer((Func<IServiceProvider, IComplexityAnalyzer>)null, (_, _) => { }));
        }
        #endregion

        #region - AddErrorInfoProvider -
        [Fact]
        public void AddErrorInfoProvider_Default1()
        {
            var action = MockSetupConfigure1<ErrorInfoProviderOptions>();
            MockSetupRegister<IErrorInfoProvider, ErrorInfoProvider>();
            _builder.AddErrorInfoProvider(action);
            Verify();
        }

        [Fact]
        public void AddErrorInfoProvider_Default2()
        {
            var action = MockSetupConfigure2<ErrorInfoProviderOptions>();
            MockSetupRegister<IErrorInfoProvider, ErrorInfoProvider>();
            _builder.AddErrorInfoProvider(action);
            Verify();
        }

        [Fact]
        public void AddErrorInfoProvider_DefaultNull()
        {
            MockSetupConfigureNull<ErrorInfoProviderOptions>();
            MockSetupRegister<IErrorInfoProvider, ErrorInfoProvider>();
            _builder.AddErrorInfoProvider();
            _builder.AddErrorInfoProvider((Action<ErrorInfoProviderOptions, IServiceProvider>)null);
            Verify();
        }

        [Fact]
        public void AddErrorInfoProvider_Typed()
        {
            MockSetupRegister<IErrorInfoProvider, TestErrorInfoProvider>();
            _builder.AddErrorInfoProvider<TestErrorInfoProvider>();
            Verify();
        }

        [Fact]
        public void AddErrorInfoProvider_Instance()
        {
            var instance = new TestErrorInfoProvider();
            MockSetupRegister<IErrorInfoProvider>(instance);
            _builder.AddErrorInfoProvider(instance);
            Verify();
        }

        [Fact]
        public void AddErrorInfoProvider_Factory()
        {
            var factory = MockSetupRegister<IErrorInfoProvider>();
            _builder.AddErrorInfoProvider(factory);
            Verify();
        }

        [Fact]
        public void AddErrorInfoProvider_Null()
        {
            Should.Throw<ArgumentNullException>(() => _builder.AddErrorInfoProvider((IErrorInfoProvider)null));
            Should.Throw<ArgumentNullException>(() => _builder.AddErrorInfoProvider((Func<IServiceProvider, IErrorInfoProvider>)null));
        }
        #endregion

        #region - AddGraphTypes -
        [Fact]
        public void AddGraphTypes()
        {
            var typeList = new Type[] {
                typeof(MyGraph),
                typeof(MyScalar),
                typeof(IGraphType),
                typeof(Class1),
            };
            var mockAssembly = new Mock<MockableAssembly>(MockBehavior.Strict);
            mockAssembly.Setup(x => x.GetTypes()).Returns(typeList).Verifiable();
            var assembly = mockAssembly.Object;

            _builderMock.Setup(b => b.TryRegister(typeof(EnumerationGraphType<>), typeof(EnumerationGraphType<>), ServiceLifetime.Transient)).Returns(_builder).Verifiable();
            _builderMock.Setup(b => b.TryRegister(typeof(ConnectionType<>), typeof(ConnectionType<>), ServiceLifetime.Transient)).Returns(_builder).Verifiable();
            _builderMock.Setup(b => b.TryRegister(typeof(ConnectionType<,>), typeof(ConnectionType<,>), ServiceLifetime.Transient)).Returns(_builder).Verifiable();
            _builderMock.Setup(b => b.TryRegister(typeof(EdgeType<>), typeof(EdgeType<>), ServiceLifetime.Transient)).Returns(_builder).Verifiable();
            _builderMock.Setup(b => b.TryRegister(typeof(InputObjectGraphType<>), typeof(InputObjectGraphType<>), ServiceLifetime.Transient)).Returns(_builder).Verifiable();
            _builderMock.Setup(b => b.TryRegister(typeof(AutoRegisteringInputObjectGraphType<>), typeof(AutoRegisteringInputObjectGraphType<>), ServiceLifetime.Transient)).Returns(_builder).Verifiable();
            _builderMock.Setup(b => b.TryRegister(typeof(AutoRegisteringObjectGraphType<>), typeof(AutoRegisteringObjectGraphType<>), ServiceLifetime.Transient)).Returns(_builder).Verifiable();

            _builderMock.Setup(b => b.TryRegister(typeof(MyGraph), typeof(MyGraph), ServiceLifetime.Transient)).Returns(_builder).Verifiable();
            _builderMock.Setup(b => b.TryRegister(typeof(MyScalar), typeof(MyScalar), ServiceLifetime.Transient)).Returns(_builder).Verifiable();

            _builder.AddGraphTypes(assembly);
            Verify();
        }

        [Fact]
        public void AddGraphTypes_StarWars()
        {
            _builderMock.Setup(b => b.TryRegister(typeof(EnumerationGraphType<>), typeof(EnumerationGraphType<>), ServiceLifetime.Transient)).Returns(_builder).Verifiable();
            _builderMock.Setup(b => b.TryRegister(typeof(ConnectionType<>), typeof(ConnectionType<>), ServiceLifetime.Transient)).Returns(_builder).Verifiable();
            _builderMock.Setup(b => b.TryRegister(typeof(ConnectionType<,>), typeof(ConnectionType<,>), ServiceLifetime.Transient)).Returns(_builder).Verifiable();
            _builderMock.Setup(b => b.TryRegister(typeof(EdgeType<>), typeof(EdgeType<>), ServiceLifetime.Transient)).Returns(_builder).Verifiable();
            _builderMock.Setup(b => b.TryRegister(typeof(InputObjectGraphType<>), typeof(InputObjectGraphType<>), ServiceLifetime.Transient)).Returns(_builder).Verifiable();
            _builderMock.Setup(b => b.TryRegister(typeof(AutoRegisteringInputObjectGraphType<>), typeof(AutoRegisteringInputObjectGraphType<>), ServiceLifetime.Transient)).Returns(_builder).Verifiable();
            _builderMock.Setup(b => b.TryRegister(typeof(AutoRegisteringObjectGraphType<>), typeof(AutoRegisteringObjectGraphType<>), ServiceLifetime.Transient)).Returns(_builder).Verifiable();

            _builderMock.Setup(b => b.TryRegister(typeof(GraphQL.StarWars.Types.CharacterInterface), typeof(GraphQL.StarWars.Types.CharacterInterface), ServiceLifetime.Transient)).Returns(_builder).Verifiable();
            _builderMock.Setup(b => b.TryRegister(typeof(GraphQL.StarWars.Types.DroidType), typeof(GraphQL.StarWars.Types.DroidType), ServiceLifetime.Transient)).Returns(_builder).Verifiable();
            _builderMock.Setup(b => b.TryRegister(typeof(GraphQL.StarWars.Types.HumanInputType), typeof(GraphQL.StarWars.Types.HumanInputType), ServiceLifetime.Transient)).Returns(_builder).Verifiable();
            _builderMock.Setup(b => b.TryRegister(typeof(GraphQL.StarWars.Types.HumanType), typeof(GraphQL.StarWars.Types.HumanType), ServiceLifetime.Transient)).Returns(_builder).Verifiable();
            _builderMock.Setup(b => b.TryRegister(typeof(GraphQL.StarWars.Types.EpisodeEnum), typeof(GraphQL.StarWars.Types.EpisodeEnum), ServiceLifetime.Transient)).Returns(_builder).Verifiable();
            _builderMock.Setup(b => b.TryRegister(typeof(GraphQL.StarWars.StarWarsQuery), typeof(GraphQL.StarWars.StarWarsQuery), ServiceLifetime.Transient)).Returns(_builder).Verifiable();
            _builderMock.Setup(b => b.TryRegister(typeof(GraphQL.StarWars.StarWarsMutation), typeof(GraphQL.StarWars.StarWarsMutation), ServiceLifetime.Transient)).Returns(_builder).Verifiable();

            _builder.AddGraphTypes(typeof(GraphQL.StarWars.Types.Droid).Assembly);
            Verify();
        }

        [Fact]
        public void AddGraphTypes_CallingAssembly()
        {
            var builderMock = new Mock<IGraphQLBuilder>(MockBehavior.Loose);
            builderMock.Setup(b => b.TryRegister(typeof(MyGraph), typeof(MyGraph), ServiceLifetime.Transient)).Returns(builderMock.Object).Verifiable();
            builderMock.Object.AddGraphTypes();
            builderMock.Verify();
        }

        [Fact]
        public void AddGraphTypes_Null()
        {
            Should.Throw<ArgumentNullException>(() => _builder.AddGraphTypes(null));
        }
        #endregion

        #region - AddClrTypeMappings -
        [Fact]
        public void AddClrTypeMappings()
        {
            var mockSchema = AddClrTypeMappings_Setup();
            _builder.AddClrTypeMappings();
            Verify();
            mockSchema.Verify();
            mockSchema.VerifyNoOtherCalls();
        }

        [Fact]
        public void AddClrTypeMappings_Assembly()
        {
            var mockSchema = AddClrTypeMappings_Setup();
            _builder.AddClrTypeMappings(System.Reflection.Assembly.GetExecutingAssembly());
            Verify();
            mockSchema.Verify();
            mockSchema.VerifyNoOtherCalls();
        }

        private Mock<ISchema> AddClrTypeMappings_Setup()
        {
            //note: this does not test the functionality of AssemblyExtensions.GetClrTypeMappings
            var typeMappings = System.Reflection.Assembly.GetExecutingAssembly().GetClrTypeMappings();
            typeMappings.Count.ShouldBeGreaterThan(0); //ensure we are testing SOMETHING
            var mockSchema = new Mock<ISchema>(MockBehavior.Strict);
            foreach (var typeMapping in typeMappings)
            {
                mockSchema.Setup(s => s.RegisterTypeMapping(typeMapping.ClrType, typeMapping.GraphType)).Verifiable();
            }

            _builderMock.Setup(b => b.Register(typeof(IConfigureSchema), It.IsAny<object>()))
                .Returns<Type, IConfigureSchema>((_, action) =>
                {
                    action.Configure(mockSchema.Object, null);
                    return _builder;
                }).Verifiable();

            return mockSchema;
        }

        [Fact]
        public void AddClrTypeMappings_Null()
        {
            Should.Throw<ArgumentNullException>(() => _builder.AddClrTypeMappings(null));
        }
        #endregion

        #region - AddDocumentListener -
        [Theory]
        [InlineData(ServiceLifetime.Singleton)]
        [InlineData(ServiceLifetime.Scoped)]
        [InlineData(ServiceLifetime.Transient)]
        public void AddDocumentListener(ServiceLifetime serviceLifetime)
        {
            MockSetupRegister<MyDocumentListener, MyDocumentListener>(serviceLifetime);
            MockSetupRegister<IDocumentExecutionListener, MyDocumentListener>(serviceLifetime);
            var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
            mockServiceProvider.Setup(sp => sp.GetService(typeof(MyDocumentListener))).Returns(new MyDocumentListener()).Verifiable();
            var getOpts = MockSetupConfigureExecution(mockServiceProvider.Object);
            if (serviceLifetime == ServiceLifetime.Singleton)
            {
                //verify default service lifetime
                _builder.AddDocumentListener<MyDocumentListener>();
            }
            else
            {
                _builder.AddDocumentListener<MyDocumentListener>(serviceLifetime);
            }
            var opts = getOpts();
            opts.Listeners.Count.ShouldBe(1);
            opts.Listeners[0].ShouldBeOfType<MyDocumentListener>();
            mockServiceProvider.Verify();
            mockServiceProvider.VerifyNoOtherCalls();
            Verify();
        }

        [Fact]
        public void AddDocumentListener_Instance()
        {
            var instance = new MyDocumentListener();
            MockSetupRegister<IDocumentExecutionListener>(instance);
            MockSetupRegister(instance);
            var getOpts = MockSetupConfigureExecution();
            _builder.AddDocumentListener(instance);
            var opts = getOpts();
            opts.Listeners.Count.ShouldBe(1);
            opts.Listeners[0].ShouldBe(instance);
            Verify();
        }

        [Fact]
        public void AddDocumentListener_Factory()
        {
            var instance = new MyDocumentListener();
            Func<IServiceProvider, MyDocumentListener> factory = services => instance;
            _builderMock.Setup(b => b.Register(typeof(IDocumentExecutionListener), factory, ServiceLifetime.Singleton)).Returns(_builder).Verifiable();
            _builderMock.Setup(b => b.Register(typeof(MyDocumentListener), factory, ServiceLifetime.Singleton)).Returns(_builder).Verifiable();
            var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
            mockServiceProvider.Setup(sp => sp.GetService(typeof(MyDocumentListener))).Returns(instance).Verifiable();
            var getOpts = MockSetupConfigureExecution(mockServiceProvider.Object);
            _builder.AddDocumentListener(factory);
            var opts = getOpts();
            opts.Listeners.Count.ShouldBe(1);
            opts.Listeners[0].ShouldBe(instance);
            mockServiceProvider.Verify();
            mockServiceProvider.VerifyNoOtherCalls();
            Verify();
        }

        [Fact]
        public void AddDocumentListener_Null()
        {
            Should.Throw<ArgumentNullException>(() => _builder.AddDocumentListener((MyDocumentListener)null));
            Should.Throw<ArgumentNullException>(() => _builder.AddDocumentListener((Func<IServiceProvider, MyDocumentListener>)null));
        }
        #endregion

        #region - AddMiddleware -
        [Theory]
        [InlineData(false, false, ServiceLifetime.Transient)]
        [InlineData(false, false, ServiceLifetime.Singleton)]
        [InlineData(true, false, ServiceLifetime.Transient)]
        [InlineData(true, false, ServiceLifetime.Singleton)]
        [InlineData(false, true, ServiceLifetime.Transient)]
        [InlineData(false, true, ServiceLifetime.Singleton)]
        [InlineData(true, true, ServiceLifetime.Transient)]
        [InlineData(true, true, ServiceLifetime.Singleton)]
        public void AddMiddleware(bool install, bool usePredicate, ServiceLifetime serviceLifetime)
        {
            var instance = new MyMiddleware();
            MockSetupRegister<IFieldMiddleware, MyMiddleware>(serviceLifetime);
            MockSetupRegister<MyMiddleware, MyMiddleware>(serviceLifetime);
            var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
            var schema = new TestSchema();
            Action runSchemaConfigs = null;
            if (install || usePredicate)
            {
                runSchemaConfigs = MockSetupConfigureSchema(schema, mockServiceProvider.Object);
            }
            if (install)
            {
                mockServiceProvider.Setup(sp => sp.GetService(typeof(MyMiddleware))).Returns(instance).Verifiable();
            }
            if (install == true && usePredicate == false && serviceLifetime == ServiceLifetime.Transient)
            {
                //verify that defaults parameters are configured appropriately
                _builder.AddMiddleware<MyMiddleware>();
            }
            else if (usePredicate)
            {
                _builder.AddMiddleware<MyMiddleware>((services, schema) => install, serviceLifetime);
            }
            else
            {
                _builder.AddMiddleware<MyMiddleware>(install, serviceLifetime);
            }
            runSchemaConfigs?.Invoke();
            FieldMiddlewareDelegate fieldResolver = _ => null;
            var middlewareTransform = schema.FieldMiddleware.Build();
            if (middlewareTransform != null)
            {
                fieldResolver = middlewareTransform(fieldResolver);
            }
            fieldResolver(null);
            instance.RanMiddleware.ShouldBe(install);
            mockServiceProvider.Verify();
            Verify();
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public void AddMiddleware_Instance(bool install, bool usePredicate)
        {
            var instance = new MyMiddleware();
            MockSetupRegister<IFieldMiddleware>(instance);
            MockSetupRegister(instance);
            var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
            var schema = new TestSchema();
            Action runSchemaConfigs = null;
            if (install || usePredicate)
            {
                runSchemaConfigs = MockSetupConfigureSchema(schema, mockServiceProvider.Object);
            }
            if (install == true && usePredicate == false)
            {
                //verify that defaults parameters are configured appropriately
                _builder.AddMiddleware(instance);
            }
            else if (usePredicate)
            {
                _builder.AddMiddleware(instance, (services, schema) => install);
            }
            else
            {
                _builder.AddMiddleware(instance, install);
            }
            runSchemaConfigs?.Invoke();
            FieldMiddlewareDelegate fieldResolver = _ => null;
            var middlewareTransform = schema.FieldMiddleware.Build();
            if (middlewareTransform != null)
            {
                fieldResolver = middlewareTransform(fieldResolver);
            }
            fieldResolver(null);
            instance.RanMiddleware.ShouldBe(install);
            mockServiceProvider.Verify();
            Verify();
        }

        [Fact]
        public void AddMiddleware_Scoped()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => _builder.AddMiddleware<MyMiddleware>(serviceLifetime: ServiceLifetime.Scoped));
            Should.Throw<ArgumentOutOfRangeException>(() => _builder.AddMiddleware<MyMiddleware>((_, _) => false, ServiceLifetime.Scoped));
        }

        [Fact]
        public void AddMiddleware_Null()
        {
            Should.Throw<ArgumentNullException>(() => _builder.AddMiddleware<MyMiddleware>(installPredicate: null));
            Should.Throw<ArgumentNullException>(() => _builder.AddMiddleware((MyMiddleware)null));
            Should.Throw<ArgumentNullException>(() => _builder.AddMiddleware(new MyMiddleware(), installPredicate: null));
            Should.Throw<ArgumentNullException>(() => _builder.AddMiddleware((MyMiddleware)null, (_, _) => true));
        }
        #endregion

        #region - AddDocumentCache -
        [Fact]
        public void AddDocumentCache()
        {
            MockSetupRegister<IDocumentCache, TestDocumentCache>();
            _builder.AddDocumentCache<TestDocumentCache>();
            Verify();
        }

        [Fact]
        public void AddDocumentCache_Instance()
        {
            var instance = new TestDocumentCache();
            MockSetupRegister<IDocumentCache>(instance);
            _builder.AddDocumentCache(instance);
            Verify();
        }

        [Fact]
        public void AddDocumentCache_Factory()
        {
            var factory = MockSetupRegister<IDocumentCache>();
            _builder.AddDocumentCache(factory);
            Verify();
        }

        [Fact]
        public void AddDocumentCache_Null()
        {
            Should.Throw<ArgumentNullException>(() => _builder.AddDocumentCache((TestDocumentCache)null));
            Should.Throw<ArgumentNullException>(() => _builder.AddDocumentCache((Func<IServiceProvider, TestDocumentCache>)null));
        }
        #endregion

        #region - AddDocumentWriter -
        [Fact]
        public void AddDocumentWriter()
        {
            MockSetupRegister<IDocumentWriter, SystemTextJson.DocumentWriter>();
            _builder.AddDocumentWriter<SystemTextJson.DocumentWriter>();
            Verify();
        }

        [Fact]
        public void AddDocumentWriter_Instance()
        {
            var instance = new SystemTextJson.DocumentWriter();
            MockSetupRegister<IDocumentWriter>(instance);
            _builder.AddDocumentWriter(instance);
            Verify();
        }

        [Fact]
        public void AddDocumentWriter_Factory()
        {
            var factory = MockSetupRegister<IDocumentWriter>();
            _builder.AddDocumentWriter(factory);
            Verify();
        }

        [Fact]
        public void AddDocumentWriter_Null()
        {
            Should.Throw<ArgumentNullException>(() => _builder.AddDocumentWriter((SystemTextJson.DocumentWriter)null));
            Should.Throw<ArgumentNullException>(() => _builder.AddDocumentWriter((Func<IServiceProvider, SystemTextJson.DocumentWriter>)null));
        }
        #endregion

        #region - ConfigureSchema and ConfigureExecution -
        [Fact]
        public void ConfigureSchema()
        {
            bool ran = false;
            var schema = new TestSchema();
            var execute = MockSetupConfigureSchema(schema);
            _builder.ConfigureSchema(schema2 =>
            {
                schema2.ShouldBe(schema);
                ran = true;
            });
            execute();
            ran.ShouldBeTrue();
            Verify();
        }

        [Fact]
        public void ConfigureSchema2()
        {
            bool ran = false;
            var schema = new TestSchema();
            var execute = MockSetupConfigureSchema(schema);
            _builder.ConfigureSchema((schema2, services) =>
            {
                schema2.ShouldBe(schema);
                ran = true;
            });
            execute();
            ran.ShouldBeTrue();
            Verify();
        }

        [Fact]
        public void ConfigureExecution()
        {
            bool ran = false;
            var execute = MockSetupConfigureExecution();
            _builder.ConfigureExecution(opts =>
            {
                opts.EnableMetrics.ShouldBeFalse();
                opts.EnableMetrics = true;
                ran = true;
            });
            var opts = execute();
            ran.ShouldBeTrue();
            opts.EnableMetrics.ShouldBeTrue();
            Verify();
        }

        [Fact]
        public void ConfigureSchema_Null()
        {
            Should.Throw<ArgumentNullException>(() => _builder.ConfigureSchema((Action<ISchema>)null));
            Should.Throw<ArgumentNullException>(() => _builder.ConfigureSchema((Action<ISchema, IServiceProvider>)null));
        }

        [Fact]
        public void ConfigureExecution_Null()
        {
            Should.Throw<ArgumentNullException>(() => _builder.ConfigureExecution(null));
        }
        #endregion

        #region - AddValidationRule -
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AddValidationRule(bool useForCachedDocuments)
        {
            var instance = new MyValidationRule();
            MockSetupRegister<MyValidationRule, MyValidationRule>();
            MockSetupRegister<IValidationRule, MyValidationRule>();
            var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
            mockServiceProvider.Setup(s => s.GetService(typeof(MyValidationRule))).Returns(instance).Verifiable();
            var getOpts = MockSetupConfigureExecution(mockServiceProvider.Object);
            if (useForCachedDocuments)
            {
                //verify default argument value
                _builder.AddValidationRule<MyValidationRule>(true);
            }
            else
            {
                _builder.AddValidationRule<MyValidationRule>();
            }
            var opts = getOpts();
            opts.ValidationRules.ShouldNotBeNull();
            opts.ValidationRules.ShouldContain(instance);
            if (useForCachedDocuments)
            {
                opts.CachedDocumentValidationRules.ShouldNotBeNull();
                opts.CachedDocumentValidationRules.ShouldContain(instance);
            }
            else
            {
                if (opts.CachedDocumentValidationRules != null)
                    opts.CachedDocumentValidationRules.ShouldBeEmpty();
            }
            mockServiceProvider.Verify();
            Verify();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AddValidationRule_Instance(bool useForCachedDocuments)
        {
            var instance = new MyValidationRule();
            MockSetupRegister<IValidationRule>(instance);
            MockSetupRegister(instance);
            var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
            var getOpts = MockSetupConfigureExecution(mockServiceProvider.Object);
            if (useForCachedDocuments)
            {
                //verify default argument value
                _builder.AddValidationRule(instance, true);
            }
            else
            {
                _builder.AddValidationRule(instance);
            }
            var opts = getOpts();
            opts.ValidationRules.ShouldNotBeNull();
            opts.ValidationRules.ShouldContain(instance);
            if (useForCachedDocuments)
            {
                opts.CachedDocumentValidationRules.ShouldNotBeNull();
                opts.CachedDocumentValidationRules.ShouldContain(instance);
            }
            else
            {
                opts.CachedDocumentValidationRules.ShouldBeNull();
            }
            mockServiceProvider.Verify();
            Verify();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AddValidationRule_Factory(bool useForCachedDocuments)
        {
            var instance = new MyValidationRule();
            Func<IServiceProvider, MyValidationRule> factory = _ => instance;
            _builderMock.Setup(b => b.Register(typeof(IValidationRule), factory, ServiceLifetime.Singleton)).Returns(_builder).Verifiable();
            _builderMock.Setup(b => b.Register(typeof(MyValidationRule), factory, ServiceLifetime.Singleton)).Returns(_builder).Verifiable();
            var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
            mockServiceProvider.Setup(s => s.GetService(typeof(MyValidationRule))).Returns(instance).Verifiable();
            var getOpts = MockSetupConfigureExecution(mockServiceProvider.Object);
            if (useForCachedDocuments)
            {
                //verify default argument value
                _builder.AddValidationRule(factory, true);
            }
            else
            {
                _builder.AddValidationRule(factory);
            }
            var opts = getOpts();
            opts.ValidationRules.ShouldNotBeNull();
            opts.ValidationRules.ShouldContain(instance);
            if (useForCachedDocuments)
            {
                opts.CachedDocumentValidationRules.ShouldNotBeNull();
                opts.CachedDocumentValidationRules.ShouldContain(instance);
            }
            else
            {
                opts.CachedDocumentValidationRules.ShouldBeNull();
            }
            mockServiceProvider.Verify();
            Verify();
        }

        [Fact]
        public void AddValidationRule_Null()
        {
            Should.Throw<ArgumentNullException>(() => _builder.AddValidationRule((MyValidationRule)null));
            Should.Throw<ArgumentNullException>(() => _builder.AddValidationRule((Func<IServiceProvider, MyValidationRule>)null));
        }
        #endregion

        #region - AddMetrics -
        [Theory]
        [InlineData(true, false, true, false)]
        [InlineData(false, false, true, false)]
        [InlineData(true, true, true, false)]
        [InlineData(false, true, true, false)]
        [InlineData(true, true, true, true)]
        [InlineData(true, true, false, true)]
        [InlineData(false, true, true, true)]
        [InlineData(false, true, false, true)]
        public void AddMetrics(bool enable, bool useEnablePredicate, bool install, bool useInstallPredicate)
        {
            var instance = new InstrumentFieldsMiddleware();
            MockSetupRegister<IFieldMiddleware, InstrumentFieldsMiddleware>(ServiceLifetime.Transient);
            MockSetupRegister<InstrumentFieldsMiddleware, InstrumentFieldsMiddleware>(ServiceLifetime.Transient);
            var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
            var schema = new TestSchema();
            //setup middleware
            Action runSchemaConfigs = null;
            if (install || useInstallPredicate)
            {
                runSchemaConfigs = MockSetupConfigureSchema(schema, mockServiceProvider.Object);
            }
            if (install)
            {
                mockServiceProvider.Setup(sp => sp.GetService(typeof(InstrumentFieldsMiddleware))).Returns(instance).Verifiable();
            }
            //setup execution
            Func<ExecutionOptions> getOptions = () => new ExecutionOptions();
            if (enable || useEnablePredicate)
            {
                getOptions = MockSetupConfigureExecution(mockServiceProvider.Object);
            }
            //test
            if (enable == true && useEnablePredicate == false && useInstallPredicate == false)
            {
                //verify that defaults parameters are configured appropriately
                _builder.AddMetrics();
            }
            else if (useInstallPredicate)
            {
                _builder.AddMetrics(opts => enable, (services, schema) => install);
            }
            else if (useEnablePredicate)
            {
                _builder.AddMetrics(opts => enable);
            }
            else
            {
                _builder.AddMetrics(enable);
            }
            //verify
            runSchemaConfigs?.Invoke();
            var options = getOptions();
            FieldMiddlewareDelegate fieldResolver = _ => null;
            (schema.FieldMiddleware.Build() != null).ShouldBe(install);
            options.EnableMetrics.ShouldBe(enable);
            mockServiceProvider.Verify();
            Verify();
        }

        [Fact]
        public void AddMetrics_Null()
        {
            Should.Throw<ArgumentNullException>(() => _builder.AddMetrics(enablePredicate: null));
            Should.Throw<ArgumentNullException>(() => _builder.AddMetrics(null, (_, _) => true));
            Should.Throw<ArgumentNullException>(() => _builder.AddMetrics(_ => true, null));
        }
        #endregion

        #region - GraphQL.MemoryCache: AddMemoryCache -
        [Fact]
        public void AddMemoryCache()
        {
            MockSetupRegister<IDocumentCache, MemoryDocumentCache>();
            MockSetupConfigureNull<MemoryDocumentCacheOptions>();
            _builder.AddMemoryCache();
            Verify();
        }

        [Fact]
        public void AddMemoryCache_Options1()
        {
            MockSetupRegister<IDocumentCache, MemoryDocumentCache>();
            var options = MockSetupConfigure1<MemoryDocumentCacheOptions>();
            _builder.AddMemoryCache(options);
            Verify();
        }

        [Fact]
        public void AddMemoryCache_Options2()
        {
            MockSetupRegister<IDocumentCache, MemoryDocumentCache>();
            var options = MockSetupConfigure2<MemoryDocumentCacheOptions>();
            _builder.AddMemoryCache(options);
            Verify();
        }
        #endregion

        #region - GraphQL.SystemTextJson: AddSystemTextJson -
        [Fact]
        public void AddSystemTextJson()
        {
            MockSetupRegister<IDocumentWriter, SystemTextJson.DocumentWriter>();
            MockSetupConfigureNull<System.Text.Json.JsonSerializerOptions>();
            _builder.AddSystemTextJson();
            Verify();
        }

        [Fact]
        public void AddSystemTextJson_Options1()
        {
            MockSetupRegister<IDocumentWriter, SystemTextJson.DocumentWriter>();
            var options = MockSetupConfigure1<System.Text.Json.JsonSerializerOptions>();
            _builder.AddSystemTextJson(options);
            Verify();
        }

        [Fact]
        public void AddSystemTextJson_Options2()
        {
            MockSetupRegister<IDocumentWriter, SystemTextJson.DocumentWriter>();
            var options = MockSetupConfigure2<System.Text.Json.JsonSerializerOptions>();
            _builder.AddSystemTextJson(options);
            Verify();
        }
        #endregion

        #region - GraphQL.NewtonsoftJson: AddNewtonsoftJson -
        [Fact]
        public void AddNewtonsoftJson()
        {
            MockSetupRegister<IDocumentWriter, NewtonsoftJson.DocumentWriter>();
            MockSetupConfigureNull<Newtonsoft.Json.JsonSerializerSettings>();
            _builder.AddNewtonsoftJson();
            Verify();
        }

        [Fact]
        public void AddNewtonsoftJson_Options1()
        {
            MockSetupRegister<IDocumentWriter, NewtonsoftJson.DocumentWriter>();
            var options = MockSetupConfigure1<Newtonsoft.Json.JsonSerializerSettings>();
            _builder.AddNewtonsoftJson(options);
            Verify();
        }

        [Fact]
        public void AddNewtonsoftJson_Options2()
        {
            MockSetupRegister<IDocumentWriter, NewtonsoftJson.DocumentWriter>();
            var options = MockSetupConfigure2<Newtonsoft.Json.JsonSerializerSettings>();
            _builder.AddNewtonsoftJson(options);
            Verify();
        }
        #endregion

        #region - GraphQL.SystemReactive: AddSubscriptionDocumentExecuter -
        [Fact]
        public void AddSubscriptionDocumentExecuter()
        {
            MockSetupRegister<IDocumentExecuter, SubscriptionDocumentExecuter>();
            _builder.AddSubscriptionDocumentExecuter();
        }
        #endregion

        private class Class1 : Interface1
        {
            public int Value { get; set; }
        }

        private interface Interface1
        {
        }

        public class MockableAssembly : System.Reflection.Assembly
        {
        }

        private class TestSchema : Schema
        {
        }

        private class TestDocumentExecuter : DocumentExecuter
        {
        }

        private class TestComplexityAnalyzer : ComplexityAnalyzer
        {
        }

        private class TestErrorInfoProvider : ErrorInfoProvider
        {
        }

        private class MyGraph : ObjectGraphType
        {
        }

        private class MyScalar : IntGraphType
        {
        }

        private class MyDocumentListener : DocumentExecutionListenerBase
        {
        }

        private class MyMiddleware : IFieldMiddleware
        {
            public bool RanMiddleware { get; set; } = false;
            public Task<object> Resolve(IResolveFieldContext context, FieldMiddlewareDelegate next)
            {
                RanMiddleware = true;
                return next(context);
            }
        }

        private class TestDocumentCache : MemoryDocumentCache
        {
        }

        private class MyValidationRule : IValidationRule
        {
            public Task<INodeVisitor> ValidateAsync(ValidationContext context) => throw new NotImplementedException();
        }
    }
}
