using System;
using System.Threading.Tasks;
using GraphQL.Caching;
using GraphQL.DI;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Types;
using GraphQL.Types.Relay;
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

        private void MockSetupRegister<TService>(TService instance, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TService : class
        {
            _builderMock.Setup(b => b.Register(serviceLifetime, It.IsAny<Func<IServiceProvider, TService>>())).Returns<ServiceLifetime, Func<IServiceProvider, TService>>((_, factory) =>
            {
                factory(null).ShouldBe(instance);
                return _builder;
            }).Verifiable();
        }

        private Func<IServiceProvider, TService> MockSetupRegister<TService>(ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TService : class
        {
            Func<IServiceProvider, TService> factory = _ => null;
            _builderMock.Setup(b => b.Register(serviceLifetime, factory)).Returns(_builder).Verifiable();
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
            _builderMock.Setup(b => b.Register(ServiceLifetime.Singleton, It.IsAny<Func<IServiceProvider, Action<ExecutionOptions>>>()))
                .Returns<ServiceLifetime, Func<IServiceProvider, Action<ExecutionOptions>>>((lifetime, actionFactory) =>
                {
                    var action = actionFactory(null);
                    var actions2 = actions;
                    actions = opts =>
                    {
                        actions2(opts);
                        action(opts);
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
            _builderMock.Setup(b => b.Register(ServiceLifetime.Singleton, It.IsAny<Func<IServiceProvider, Action<ISchema, IServiceProvider>>>()))
                .Returns<ServiceLifetime, Func<IServiceProvider, Action<ISchema, IServiceProvider>>>((lifetime, actionFactory) =>
                {
                    var action = actionFactory(null);
                    var actions2 = actions;
                    actions = (opts, services) =>
                    {
                        actions2(opts, services);
                        action(opts, services);
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

        #region - AddComplexityAnalyzer -
        [Fact]
        private void AddComplexityAnalyzer()
        {
            var action = MockSetupConfigure1<ComplexityConfiguration>();
            _builder.AddComplexityAnalyzer(action);
            Verify();
        }

        [Fact]
        private void AddComplexityAnalyzer2()
        {
            var action = MockSetupConfigure2<ComplexityConfiguration>();
            _builder.AddComplexityAnalyzer(action);
            Verify();
        }

        [Fact]
        private void AddComplexityAnalyzer_Null()
        {
            _builder.AddComplexityAnalyzer();
            _builder.AddComplexityAnalyzer((Action<ComplexityConfiguration, IServiceProvider>)null);
            Verify();
        }

        [Fact]
        private void AddComplexityAnalyzer_Typed()
        {
            MockSetupRegister<IComplexityAnalyzer, TestComplexityAnalyzer>();
            var action = MockSetupConfigure1<ComplexityConfiguration>();
            _builder.AddComplexityAnalyzer<TestComplexityAnalyzer>(action);
            Verify();
        }

        [Fact]
        private void AddComplexityAnalyzer_Typed2()
        {
            MockSetupRegister<IComplexityAnalyzer, TestComplexityAnalyzer>();
            var action = MockSetupConfigure2<ComplexityConfiguration>();
            _builder.AddComplexityAnalyzer<TestComplexityAnalyzer>(action);
            Verify();
        }

        [Fact]
        private void AddComplexityAnalyzer_Typed2_Null()
        {
            MockSetupRegister<IComplexityAnalyzer, TestComplexityAnalyzer>();
            _builder.AddComplexityAnalyzer<TestComplexityAnalyzer>();
            _builder.AddComplexityAnalyzer<TestComplexityAnalyzer>((Action<ComplexityConfiguration, IServiceProvider>)null);
            Verify();
        }

        [Fact]
        private void AddComplexityAnalyzer_Instance()
        {
            var instance = new TestComplexityAnalyzer();
            MockSetupRegister<IComplexityAnalyzer>(instance);
            var action = MockSetupConfigure1<ComplexityConfiguration>();
            _builder.AddComplexityAnalyzer(instance, action);
            Verify();
        }

        [Fact]
        private void AddComplexityAnalyzer_Instance2()
        {
            var instance = new TestComplexityAnalyzer();
            MockSetupRegister<IComplexityAnalyzer>(instance);
            var action = MockSetupConfigure2<ComplexityConfiguration>();
            _builder.AddComplexityAnalyzer(instance, action);
            Verify();
        }

        [Fact]
        private void AddComplexityAnalyzer_Instance2_Null()
        {
            var instance = new TestComplexityAnalyzer();
            MockSetupRegister<IComplexityAnalyzer>(instance);
            _builder.AddComplexityAnalyzer(instance);
            _builder.AddComplexityAnalyzer(instance, (Action<ComplexityConfiguration, IServiceProvider>)null);
            Verify();
        }

        [Fact]
        private void AddComplexityAnalyzer_InstanceNull()
        {
            Should.Throw<ArgumentNullException>(() => _builder.AddComplexityAnalyzer((IComplexityAnalyzer)null));
        }

        [Fact]
        private void AddComplexityAnalyzer_Factory()
        {
            var factory = MockSetupRegister<IComplexityAnalyzer>();
            var action = MockSetupConfigure1<ComplexityConfiguration>();
            _builder.AddComplexityAnalyzer(factory, action);
            Verify();
        }

        [Fact]
        private void AddComplexityAnalyzer_Factory2()
        {
            var factory = MockSetupRegister<IComplexityAnalyzer>();
            var action = MockSetupConfigure2<ComplexityConfiguration>();
            _builder.AddComplexityAnalyzer(factory, action);
            Verify();
        }

        [Fact]
        private void AddComplexityAnalyzer_Factory2_Null()
        {
            var factory = MockSetupRegister<IComplexityAnalyzer>();
            _builder.AddComplexityAnalyzer(factory);
            _builder.AddComplexityAnalyzer(factory, (Action<ComplexityConfiguration, IServiceProvider>)null);
            Verify();
        }

        [Fact]
        private void AddComplexityAnalyzer_FactoryNull()
        {
            Should.Throw<ArgumentNullException>(() => _builder.AddComplexityAnalyzer((Func<IServiceProvider, IComplexityAnalyzer>)null));
        }
        #endregion

        #region - AddErrorInfoProvider -
        [Fact]
        private void AddErrorInfoProvider_Default1()
        {
            var action = MockSetupConfigure1<ErrorInfoProviderOptions>();
            MockSetupRegister<IErrorInfoProvider, ErrorInfoProvider>();
            _builder.AddErrorInfoProvider(action);
            Verify();
        }

        [Fact]
        private void AddErrorInfoProvider_Default2()
        {
            var action = MockSetupConfigure2<ErrorInfoProviderOptions>();
            MockSetupRegister<IErrorInfoProvider, ErrorInfoProvider>();
            _builder.AddErrorInfoProvider(action);
            Verify();
        }

        [Fact]
        private void AddErrorInfoProvider_DefaultNull()
        {
            MockSetupConfigureNull<ErrorInfoProviderOptions>();
            MockSetupRegister<IErrorInfoProvider, ErrorInfoProvider>();
            _builder.AddErrorInfoProvider();
            _builder.AddErrorInfoProvider((Action<ErrorInfoProviderOptions, IServiceProvider>)null);
            Verify();
        }

        [Fact]
        private void AddErrorInfoProvider_Typed()
        {
            MockSetupRegister<IErrorInfoProvider, TestErrorInfoProvider>();
            _builder.AddErrorInfoProvider<TestErrorInfoProvider>();
            Verify();
        }

        [Fact]
        private void AddErrorInfoProvider_Instance()
        {
            var instance = new TestErrorInfoProvider();
            MockSetupRegister<IErrorInfoProvider>(instance);
            _builder.AddErrorInfoProvider(instance);
            Verify();
        }

        [Fact]
        private void AddErrorInfoProvider_Factory()
        {
            var factory = MockSetupRegister<IErrorInfoProvider>();
            _builder.AddErrorInfoProvider(factory);
            Verify();
        }

        [Fact]
        private void AddErrorInfoProvider_Null()
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

            _builderMock.Setup(b => b.Register(ServiceLifetime.Singleton, It.IsAny<Func<IServiceProvider, Action<ISchema, IServiceProvider>>>()))
                .Returns<ServiceLifetime, Func<IServiceProvider, Action<ISchema, IServiceProvider>>>((serviceLifetime, factory) =>
                {
                    var action = factory(null);
                    action(mockSchema.Object, null);
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
            _builderMock.Setup(b => b.Register<IDocumentExecutionListener>(ServiceLifetime.Singleton, factory)).Returns(_builder).Verifiable();
            _builderMock.Setup(b => b.Register(ServiceLifetime.Singleton, factory)).Returns(_builder).Verifiable();
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
            if (middlewareTransform != null) fieldResolver = middlewareTransform(fieldResolver);
            fieldResolver(null);
            instance.RanMiddleware.ShouldBe(install);
            mockServiceProvider.Verify();
            Verify();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AddMiddleware_Instance(bool install)
        {
            var instance = new MyMiddleware();
            MockSetupRegister<IFieldMiddleware>(instance);
            MockSetupRegister(instance);
            var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
            var schema = new TestSchema();
            Action runSchemaConfigs = null;
            if (install)
            {
                runSchemaConfigs = MockSetupConfigureSchema(schema, mockServiceProvider.Object);
            }
            if (install == true)
            {
                //verify that defaults parameters are configured appropriately
                _builder.AddMiddleware(instance);
            }
            else
            {
                _builder.AddMiddleware(instance, install);
            }
            runSchemaConfigs?.Invoke();
            FieldMiddlewareDelegate fieldResolver = _ => null;
            var middlewareTransform = schema.FieldMiddleware.Build();
            if (middlewareTransform != null)
                fieldResolver = middlewareTransform(fieldResolver);
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
    }
}
