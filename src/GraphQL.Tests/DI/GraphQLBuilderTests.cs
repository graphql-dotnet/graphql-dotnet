using GraphQL.Caching;
using GraphQL.DI;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.NewtonsoftJson;
using GraphQL.Types;
using GraphQL.Types.Collections;
using GraphQL.Types.Relay;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using GraphQL.Validation.Rules.Custom;
using GraphQLParser.AST;
using Moq;

namespace GraphQL.Tests.DI;

public class GraphQLBuilderExtensionTests
{
    public interface IGraphQLBuilderAndServiceRegister : IGraphQLBuilder, IServiceRegister
    {
    }

    private readonly Mock<IGraphQLBuilderAndServiceRegister> _builderMock = new(MockBehavior.Strict);
    private IGraphQLBuilderAndServiceRegister _builder => _builderMock.Object;

    public GraphQLBuilderExtensionTests()
    {
        _builderMock.Setup(b => b.Services).Returns(_builder).Verifiable();
    }

    private void Verify()
    {
        _builderMock.Verify();
        _builderMock.VerifyNoOtherCalls();
    }

    private void MockSetupRegister<TService, TImplementation>(ServiceLifetime serviceLifetime = ServiceLifetime.Singleton, bool replace = false)
    {
        _builderMock.Setup(b => b.Register(typeof(TService), typeof(TImplementation), serviceLifetime, replace)).Returns(_builder).Verifiable();
    }

    private void MockSetupRegister<TService>(TService instance, bool replace = false)
        where TService : class
    {
        _builderMock.Setup(b => b.Register(typeof(TService), instance, replace)).Returns(_builder).Verifiable();
    }

    private Func<IServiceProvider, TService> MockSetupRegister<TService>(ServiceLifetime serviceLifetime = ServiceLifetime.Singleton, bool replace = false)
        where TService : class
    {
        Func<IServiceProvider, TService> factory = _ => null;
        _builderMock.Setup(b => b.Register(typeof(TService), factory, serviceLifetime, replace)).Returns(_builder).Verifiable();
        return factory;
    }

    private void MockSetupTryRegister<TService, TImplementation>(ServiceLifetime serviceLifetime = ServiceLifetime.Singleton, RegistrationCompareMode registrationCompareMode = RegistrationCompareMode.ServiceType)
    {
        _builderMock.Setup(b => b.TryRegister(typeof(TService), typeof(TImplementation), serviceLifetime, registrationCompareMode)).Returns(_builder).Verifiable();
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
        _builderMock.Setup(b => b.Register(typeof(IConfigureExecution), It.IsAny<object>(), false))
            .Returns<Type, IConfigureExecution, bool>((_, action, _) =>
            {
                var actions2 = actions;
                actions = opts =>
                {
                    actions2(opts);
                    action.ExecuteAsync(opts, _ => Task.FromResult<ExecutionResult>(null!)).Wait();
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
        _builderMock.Setup(b => b.Register(typeof(IConfigureSchema), It.IsAny<object>(), false))
            .Returns<Type, IConfigureSchema, bool>((_, action, _) =>
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
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Register(bool replace)
    {
        _builderMock.Setup(x => x.Services.Register(typeof(Class1), typeof(Class1), ServiceLifetime.Transient, replace)).Returns((IGraphQLBuilderAndServiceRegister)null).Verifiable();
        _builder.Services.Register<Class1>(ServiceLifetime.Transient, replace);
        Verify();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Register_Implementation(bool replace)
    {
        _builderMock.Setup(x => x.Services.Register(typeof(Interface1), typeof(Class1), ServiceLifetime.Transient, replace)).Returns((IGraphQLBuilderAndServiceRegister)null).Verifiable();
        _builder.Services.Register<Interface1, Class1>(ServiceLifetime.Transient, replace);
        Verify();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Register_Instance(bool replace)
    {
        var instance = new Class1();
        _builderMock.Setup(x => x.Services.Register(typeof(Interface1), instance, replace)).Returns(_builder).Verifiable();
        _builder.Services.Register<Interface1>(instance, replace);
        Verify();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Register_Factory(bool replace)
    {
        var factory = MockSetupRegister<Class1>(ServiceLifetime.Transient, replace);
        _builder.Services.Register(factory, ServiceLifetime.Transient, replace);
        Verify();
    }

    [Fact]
    public void Register_Null()
    {
        Should.Throw<ArgumentNullException>(() => _builder.Services.Register<Class1>(implementationInstance: null));
        Should.Throw<ArgumentNullException>(() => _builder.Services.Register<Class1>(implementationFactory: null, ServiceLifetime.Singleton));
    }

    [Fact]
    public void TryRegister()
    {
        _builderMock.Setup(x => x.Services.TryRegister(typeof(Class1), typeof(Class1), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns((IGraphQLBuilderAndServiceRegister)null).Verifiable();
        _builder.Services.TryRegister<Class1>(ServiceLifetime.Transient);
        Verify();
    }

    [Fact]
    public void TryRegisterImplementation()
    {
        _builderMock.Setup(x => x.Services.TryRegister(typeof(Interface1), typeof(Class1), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns((IGraphQLBuilderAndServiceRegister)null).Verifiable();
        _builder.Services.TryRegister<Interface1, Class1>(ServiceLifetime.Transient);
        Verify();
    }

    [Fact]
    public void TryRegister_Instance()
    {
        var instance = new Class1();
        _builderMock.Setup(x => x.Services.TryRegister(typeof(Interface1), instance, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builder.Services.TryRegister<Interface1>(instance);
        Verify();
    }

    [Fact]
    public void TryRegister_Factory()
    {
        Func<IServiceProvider, Class1> factory = _ => null;
        _builderMock.Setup(b => b.Services.TryRegister(typeof(Class1), factory, ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builder.Services.TryRegister(factory, ServiceLifetime.Transient);
        Verify();
    }

    [Fact]
    public void TryRegister_Null()
    {
        Should.Throw<ArgumentNullException>(() => _builder.Services.TryRegister<Class1>(implementationInstance: null));
        Should.Throw<ArgumentNullException>(() => _builder.Services.TryRegister<Class1>(implementationFactory: null, ServiceLifetime.Singleton));
    }

    [Fact]
    public void Configure()
    {
        _builderMock.Setup(x => x.Services.Configure(It.IsAny<Action<Class1, IServiceProvider>>())).Returns<Action<Class1, IServiceProvider>>(a =>
        {
            var c = new Class1();
            a(c, null);
            c.Value.ShouldBe(1);
            return null;
        }).Verifiable();
        _builder.Services.Configure<Class1>(x => x.Value = 1);
        Verify();
    }

    [Fact]
    public void ConfigureNull()
    {
        _builderMock.Setup(x => x.Services.Configure<Class1>(null)).Returns((IGraphQLBuilderAndServiceRegister)null).Verifiable();
        Action<Class1> arg = null;
        _builder.Services.Configure(arg);
        Verify();
    }
    #endregion

    #region - AddSchema -
    [Fact]
    public void AddSchema()
    {
        _builderMock.Setup(b => b.Register(typeof(TestSchema), typeof(TestSchema), ServiceLifetime.Singleton, false)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(ISchema), typeof(TestSchema), ServiceLifetime.Singleton, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
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
        _builderMock.Setup(b => b.Register(typeof(TestSchema), typeof(TestSchema), ServiceLifetime.Scoped, false)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(ISchema), typeof(TestSchema), ServiceLifetime.Scoped, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builder.AddSchema<TestSchema>(ServiceLifetime.Scoped);
        Verify();
    }

    [Fact]
    public void AddSchema_Factory()
    {
        Func<IServiceProvider, TestSchema> factory = _ => null;
        _builderMock.Setup(b => b.Register(typeof(TestSchema), factory, ServiceLifetime.Singleton, false)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(ISchema), factory, ServiceLifetime.Singleton, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builder.AddSchema(factory);
        Verify();
    }

    [Fact]
    public void AddSchema_Instance()
    {
        var schema = new TestSchema();
        _builderMock.Setup(b => b.Register(typeof(TestSchema), schema, false)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(ISchema), schema, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
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
    [Theory]
    [InlineData(false, false, false, false, false)]
    [InlineData(false, false, false, true, false)]
    [InlineData(false, false, false, false, true)]
    [InlineData(true, false, false, false, false)]
    [InlineData(true, false, false, true, false)]
    [InlineData(true, false, false, false, true)]
    [InlineData(true, true, false, false, false)]
    [InlineData(true, true, false, true, false)]
    [InlineData(true, true, false, false, true)]
    [InlineData(true, false, true, false, false)]
    [InlineData(true, false, true, true, false)]
    [InlineData(true, false, true, false, true)]
    public void AddComplexityAnalyzer(bool typed, bool withFactory, bool withInstance, bool withAction, bool withAction2)
    {
        var ruleInstance = new ComplexityValidationRule(new ComplexityConfiguration());
        MockSetupRegister<ComplexityValidationRule, ComplexityValidationRule>();
        MockSetupRegister<IValidationRule, ComplexityValidationRule>();
        var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        mockServiceProvider.Setup(s => s.GetService(typeof(ComplexityValidationRule))).Returns(ruleInstance).Verifiable();
        var getOpts = MockSetupConfigureExecution(mockServiceProvider.Object);
        Func<IServiceProvider, IComplexityAnalyzer> factory = null;
        MyComplexityAnalyzer instance = null;
        if (typed)
        {
            if (withFactory)
            {
                factory = MockSetupRegister<IComplexityAnalyzer>();
            }
            else if (withInstance)
            {
                instance = new MyComplexityAnalyzer();
                MockSetupRegister<IComplexityAnalyzer>(instance);
            }
            else
            {
                MockSetupRegister<IComplexityAnalyzer, MyComplexityAnalyzer>();
            }
        }
        if (withAction)
        {
            var action = MockSetupConfigure1<ComplexityConfiguration>();
            if (typed)
            {
                if (withFactory)
                    _builder.AddComplexityAnalyzer(factory, action);
                else if (withInstance)
                    _builder.AddComplexityAnalyzer(instance, action);
                else
                    _builder.AddComplexityAnalyzer<MyComplexityAnalyzer>(action);
            }
            else
            {
                _builder.AddComplexityAnalyzer(action);
            }
        }
        else
        {
            var action = withAction2 ? MockSetupConfigure2<ComplexityConfiguration>() : null;
            if (!withAction2)
                MockSetupConfigureNull<ComplexityConfiguration>();
            if (typed)
            {
                if (withFactory)
                    _builder.AddComplexityAnalyzer(factory, action);
                else if (withInstance)
                    _builder.AddComplexityAnalyzer(instance, action);
                else
                    _builder.AddComplexityAnalyzer<MyComplexityAnalyzer>(action);
            }
            else
            {
                _builder.AddComplexityAnalyzer(action);
            }
        }
        var opts = getOpts();
        opts.ValidationRules.ShouldNotBeNull();
        opts.ValidationRules.ShouldContain(ruleInstance);
        opts.CachedDocumentValidationRules?.ShouldBeEmpty();
        mockServiceProvider.Verify();
        Verify();
    }

    [Fact]
    public void AddComplexityAnalyzer_Null()
    {
        Should.Throw<ArgumentNullException>(() => _builder.AddComplexityAnalyzer((IComplexityAnalyzer)null));
        Should.Throw<ArgumentNullException>(() => _builder.AddComplexityAnalyzer((Func<IServiceProvider, IComplexityAnalyzer>)null));
    }

    private class MyComplexityAnalyzer : ComplexityAnalyzer { }
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
            typeof(MyGraphNotRegistered),
            typeof(MyGraph),
            typeof(MyScalar),
            typeof(IGraphType),
            typeof(Class1),
        };
        var mockAssembly = new Mock<MockableAssembly>(MockBehavior.Strict);
        mockAssembly.Setup(x => x.GetTypes()).Returns(typeList).Verifiable();
        var assembly = mockAssembly.Object;

        _builderMock.Setup(b => b.TryRegister(typeof(EnumerationGraphType<>), typeof(EnumerationGraphType<>), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(ConnectionType<>), typeof(ConnectionType<>), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(ConnectionType<,>), typeof(ConnectionType<,>), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(EdgeType<>), typeof(EdgeType<>), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(PageInfoType), typeof(PageInfoType), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(InputObjectGraphType<>), typeof(InputObjectGraphType<>), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(AutoRegisteringInputObjectGraphType<>), typeof(AutoRegisteringInputObjectGraphType<>), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(AutoRegisteringObjectGraphType<>), typeof(AutoRegisteringObjectGraphType<>), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();

        _builderMock.Setup(b => b.TryRegister(typeof(MyGraph), typeof(MyGraph), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(MyScalar), typeof(MyScalar), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();

        _builder.AddGraphTypes(assembly);
        Verify();
    }

    [Fact]
    public void AddGraphTypes_StarWars()
    {
        _builderMock.Setup(b => b.TryRegister(typeof(EnumerationGraphType<>), typeof(EnumerationGraphType<>), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(ConnectionType<>), typeof(ConnectionType<>), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(ConnectionType<,>), typeof(ConnectionType<,>), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(EdgeType<>), typeof(EdgeType<>), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(PageInfoType), typeof(PageInfoType), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(InputObjectGraphType<>), typeof(InputObjectGraphType<>), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(AutoRegisteringInputObjectGraphType<>), typeof(AutoRegisteringInputObjectGraphType<>), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(AutoRegisteringObjectGraphType<>), typeof(AutoRegisteringObjectGraphType<>), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();

        _builderMock.Setup(b => b.TryRegister(typeof(GraphQL.StarWars.Types.CharacterInterface), typeof(GraphQL.StarWars.Types.CharacterInterface), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(GraphQL.StarWars.Types.DroidType), typeof(GraphQL.StarWars.Types.DroidType), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(GraphQL.StarWars.Types.HumanInputType), typeof(GraphQL.StarWars.Types.HumanInputType), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(GraphQL.StarWars.Types.HumanType), typeof(GraphQL.StarWars.Types.HumanType), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(GraphQL.StarWars.Types.EpisodeEnum), typeof(GraphQL.StarWars.Types.EpisodeEnum), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(GraphQL.StarWars.StarWarsQuery), typeof(GraphQL.StarWars.StarWarsQuery), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.TryRegister(typeof(GraphQL.StarWars.StarWarsMutation), typeof(GraphQL.StarWars.StarWarsMutation), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(_builder).Verifiable();

        _builder.AddGraphTypes(typeof(GraphQL.StarWars.Types.Droid).Assembly);
        Verify();
    }

    [Fact]
    public void AddGraphTypes_CallingAssembly()
    {
        var builderMock = new Mock<IGraphQLBuilderAndServiceRegister>(MockBehavior.Loose);
        builderMock.Setup(b => b.Services.TryRegister(typeof(MyGraph), typeof(MyGraph), ServiceLifetime.Transient, RegistrationCompareMode.ServiceType)).Returns(builderMock.Object).Verifiable();
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
        var validation = AddClrTypeMappings_Setup();
        _builder.AddClrTypeMappings();
        Verify();
        validation();
    }

    [Fact]
    public void AddClrTypeMappings_Assembly()
    {
        var validation = AddClrTypeMappings_Setup();
        _builder.AddClrTypeMappings(System.Reflection.Assembly.GetExecutingAssembly());
        Verify();
        validation();
    }

    private Action AddClrTypeMappings_Setup()
    {
        //note: this does not test the functionality of AssemblyExtensions.GetClrTypeMappings
        var typeMappings = System.Reflection.Assembly.GetExecutingAssembly().GetClrTypeMappings();
        typeMappings.Count.ShouldBeGreaterThan(0); //ensure we are testing SOMETHING

        var registeredResolvers = new List<ManualGraphTypeMappingProvider>();
        _builderMock.Setup(b => b.Register(typeof(IGraphTypeMappingProvider), It.IsAny<ManualGraphTypeMappingProvider>(), false))
            .Returns<Type, ManualGraphTypeMappingProvider, bool>((_, resolver, _) =>
            {
                registeredResolvers.Add(resolver);
                return _builder;
            })
            .Verifiable();

        return () =>
        {
            foreach (var mapping in typeMappings)
            {
                registeredResolvers
                    .Any(r => r.GetGraphTypeFromClrType(mapping.ClrType, mapping.GraphType.IsInputType(), null) == mapping.GraphType)
                    .ShouldBeTrue($"The type mapping for '{mapping.ClrType.Name}' was not matched to '{mapping.GraphType.Name}'.");
            }
        };
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
        _builderMock.Setup(b => b.Register(typeof(IDocumentExecutionListener), factory, ServiceLifetime.Singleton, false)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.Register(typeof(MyDocumentListener), factory, ServiceLifetime.Singleton, false)).Returns(_builder).Verifiable();
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
            _builder.UseMiddleware<MyMiddleware>();
        }
        else if (usePredicate)
        {
            _builder.UseMiddleware<MyMiddleware>((services, schema) => install, serviceLifetime);
        }
        else
        {
            _builder.UseMiddleware<MyMiddleware>(install, serviceLifetime);
        }
        runSchemaConfigs?.Invoke();
        FieldMiddlewareDelegate fieldResolver = _ => default;
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
            _builder.UseMiddleware(instance);
        }
        else if (usePredicate)
        {
            _builder.UseMiddleware(instance, (services, schema) => install);
        }
        else
        {
            _builder.UseMiddleware(instance, install);
        }
        runSchemaConfigs?.Invoke();
        FieldMiddlewareDelegate fieldResolver = _ => default;
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
        Should.Throw<ArgumentOutOfRangeException>(() => _builder.UseMiddleware<MyMiddleware>(serviceLifetime: ServiceLifetime.Scoped));
        Should.Throw<ArgumentOutOfRangeException>(() => _builder.UseMiddleware<MyMiddleware>((_, _) => false, ServiceLifetime.Scoped));
    }

    [Fact]
    public void AddMiddleware_Null()
    {
        Should.Throw<ArgumentNullException>(() => _builder.UseMiddleware<MyMiddleware>(installPredicate: null));
        Should.Throw<ArgumentNullException>(() => _builder.UseMiddleware((MyMiddleware)null));
        Should.Throw<ArgumentNullException>(() => _builder.UseMiddleware(new MyMiddleware(), installPredicate: null));
        Should.Throw<ArgumentNullException>(() => _builder.UseMiddleware((MyMiddleware)null, (_, _) => true));
    }
    #endregion

    #region - AddSerializer -
    [Fact]
    public void AddSerializer()
    {
        MockSetupRegister<IGraphQLSerializer, SystemTextJson.GraphQLSerializer>(ServiceLifetime.Singleton, true);
        MockSetupRegister<IGraphQLTextSerializer, SystemTextJson.GraphQLSerializer>(ServiceLifetime.Singleton, true);
        _builder.AddSerializer<SystemTextJson.GraphQLSerializer>();
        Verify();
    }

    [Fact]
    public void AddSerializer_Instance()
    {
        var instance = new SystemTextJson.GraphQLSerializer();
        MockSetupRegister<IGraphQLSerializer>(instance, true);
        MockSetupRegister<IGraphQLTextSerializer>(instance, true);
        _builder.AddSerializer(instance);
        Verify();
    }

    [Fact]
    public void AddSerializer_Factory()
    {
        var factory = MockSetupRegister<IGraphQLTextSerializer>(ServiceLifetime.Singleton, true);
        _builderMock.Setup(b => b.Register(typeof(IGraphQLSerializer), factory, ServiceLifetime.Singleton, true)).Returns(_builder).Verifiable();
        _builder.AddSerializer(factory);
        Verify();
    }

    [Fact]
    public void AddSerializer_Null()
    {
        Should.Throw<ArgumentNullException>(() => _builder.AddSerializer((SystemTextJson.GraphQLSerializer)null));
        Should.Throw<ArgumentNullException>(() => _builder.AddSerializer((Func<IServiceProvider, SystemTextJson.GraphQLSerializer>)null));
    }
    #endregion

    #region - ConfigureSchema and ConfigureExecutionOptions -
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
    public void ConfigureExecutionOptions()
    {
        bool ran = false;
        var execute = MockSetupConfigureExecution();
        _builder.ConfigureExecutionOptions(opts =>
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
        Should.Throw<ArgumentNullException>(() => _builder.ConfigureExecutionOptions(null));
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
        _builderMock.Setup(b => b.Register(typeof(IValidationRule), factory, ServiceLifetime.Singleton, false)).Returns(_builder).Verifiable();
        _builderMock.Setup(b => b.Register(typeof(MyValidationRule), factory, ServiceLifetime.Singleton, false)).Returns(_builder).Verifiable();
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

    #region - AddExecutionStrategySelector -
    [Fact]
    public void AddExecutionStrategySelector()
    {
        MockSetupRegister<IExecutionStrategySelector, TestExecutionStrategySelector>();
        _builder.AddExecutionStrategySelector<TestExecutionStrategySelector>();
        Verify();
    }

    [Fact]
    public void AddExecutionStrategySelector_Instance()
    {
        var instance = new TestExecutionStrategySelector();
        MockSetupRegister<IExecutionStrategySelector>(instance);
        _builder.AddExecutionStrategySelector(instance);
        Verify();
    }

    [Fact]
    public void AddExecutionStrategySelector_Factory()
    {
        var factory = MockSetupRegister<IExecutionStrategySelector>();
        _builder.AddExecutionStrategySelector(factory);
        Verify();
    }

    [Fact]
    public void AddExecutionStrategySelector_Null()
    {
        Should.Throw<ArgumentNullException>(() => _builder.AddExecutionStrategySelector((TestExecutionStrategySelector)null));
        Should.Throw<ArgumentNullException>(() => _builder.AddExecutionStrategySelector((Func<IServiceProvider, TestExecutionStrategySelector>)null));
    }
    #endregion

    #region - AddExecutionStrategy -
    [Fact]
    public void AddExecutionStrategy()
    {
        MockSetupRegister<TestExecutionStrategy, TestExecutionStrategy>();
        _builderMock.Setup(x => x.Register(typeof(ExecutionStrategyRegistration), It.IsAny<Func<IServiceProvider, object>>(), ServiceLifetime.Singleton, false))
            .Returns<Type, Func<IServiceProvider, object>, ServiceLifetime, bool>((_, factory, _, _) =>
            {
                var instance = new TestExecutionStrategy();
                var providerMock = new Mock<IServiceProvider>(MockBehavior.Strict);
                providerMock.Setup(x => x.GetService(typeof(TestExecutionStrategy))).Returns(instance);
                var reg = factory(providerMock.Object).ShouldBeOfType<ExecutionStrategyRegistration>();
                reg.Strategy.ShouldBe(instance);
                reg.Operation.ShouldBe(OperationType.Mutation);
                return _builderMock.Object;
            }).Verifiable();
        _builder.AddExecutionStrategy<TestExecutionStrategy>(OperationType.Mutation);
        Verify();
    }

    [Fact]
    public void AddExecutionStrategy_Instance()
    {
        var instance = new TestExecutionStrategy();
        _builderMock.Setup(x => x.Register(typeof(ExecutionStrategyRegistration), It.IsAny<ExecutionStrategyRegistration>(), false))
            .Returns<Type, ExecutionStrategyRegistration, bool>((_, reg, _) =>
            {
                reg.Strategy.ShouldBe(instance);
                reg.Operation.ShouldBe(OperationType.Mutation);
                return _builderMock.Object;
            }).Verifiable();
        _builder.AddExecutionStrategy(instance, OperationType.Mutation);
        Verify();
    }

    [Fact]
    public void AddExecutionStrategy_Factory()
    {
        var instance = new TestExecutionStrategy();
        Func<IServiceProvider, IExecutionStrategy> func = (provider) => instance;
        _builderMock.Setup(x => x.Register(typeof(ExecutionStrategyRegistration), It.IsAny<Func<IServiceProvider, object>>(), ServiceLifetime.Singleton, false))
            .Returns<Type, Func<IServiceProvider, object>, ServiceLifetime, bool>((_, factory, _, _) =>
            {
                var providerMock = new Mock<IServiceProvider>(MockBehavior.Strict);
                var reg = factory(providerMock.Object).ShouldBeOfType<ExecutionStrategyRegistration>();
                reg.Strategy.ShouldBe(instance);
                reg.Operation.ShouldBe(OperationType.Mutation);
                return _builderMock.Object;
            }).Verifiable();
        _builder.AddExecutionStrategy(func, OperationType.Mutation);
        Verify();
    }

    [Fact]
    public void AddExecutionStrategy_Null()
    {
        Should.Throw<ArgumentNullException>(() => _builder.AddExecutionStrategy((TestExecutionStrategy)null, OperationType.Mutation));
        Should.Throw<ArgumentNullException>(() => _builder.AddExecutionStrategy((Func<IServiceProvider, TestExecutionStrategy>)null, OperationType.Mutation));
    }
    #endregion

    #region - GraphQL.MemoryCache: UseMemoryCache / UseAutomaticPersistedQueries -
    [Fact]
    public void UseMemoryCache()
    {
        MockSetupRegister<IConfigureExecution, MemoryDocumentCache>(ServiceLifetime.Singleton);
        MockSetupConfigureNull<MemoryDocumentCacheOptions>();
        _builder.UseMemoryCache();
        Verify();
    }

    [Fact]
    public void UseMemoryCache_Options1()
    {
        MockSetupRegister<IConfigureExecution, MemoryDocumentCache>(ServiceLifetime.Singleton);
        var options = MockSetupConfigure1<MemoryDocumentCacheOptions>();
        _builder.UseMemoryCache(options);
        Verify();
    }

    [Fact]
    public void UseMemoryCache_Options2()
    {
        MockSetupRegister<IConfigureExecution, MemoryDocumentCache>(ServiceLifetime.Singleton);
        var options = MockSetupConfigure2<MemoryDocumentCacheOptions>();
        _builder.UseMemoryCache(options);
        Verify();
    }

    [Fact]
    public void UseAutomaticPersistedQueries()
    {
        MockSetupRegister<IConfigureExecution, AutomaticPersistedQueriesExecution>(ServiceLifetime.Singleton);
        MockSetupConfigureNull<AutomaticPersistedQueriesCacheOptions>();
        _builder.UseAutomaticPersistedQueries();
        Verify();
    }

    [Fact]
    public void UseAutomaticPersistedQueries_Options1()
    {
        MockSetupRegister<IConfigureExecution, AutomaticPersistedQueriesExecution>(ServiceLifetime.Singleton);
        var options = MockSetupConfigure1<AutomaticPersistedQueriesCacheOptions>();
        _builder.UseAutomaticPersistedQueries(options);
        Verify();
    }

    [Fact]
    public void UseAutomaticPersistedQueries_Options2()
    {
        MockSetupRegister<IConfigureExecution, AutomaticPersistedQueriesExecution>(ServiceLifetime.Singleton);
        var options = MockSetupConfigure2<AutomaticPersistedQueriesCacheOptions>();
        _builder.UseAutomaticPersistedQueries(options);
        Verify();
    }
    #endregion

    #region - GraphQL.SystemTextJson: AddSystemTextJson -
    [Fact]
    public void AddSystemTextJson()
    {
        MockSetupRegister<IGraphQLSerializer, SystemTextJson.GraphQLSerializer>(ServiceLifetime.Singleton, true);
        MockSetupRegister<IGraphQLTextSerializer, SystemTextJson.GraphQLSerializer>(ServiceLifetime.Singleton, true);
        MockSetupConfigureNull<System.Text.Json.JsonSerializerOptions>();
        _builder.AddSystemTextJson();
        Verify();
    }

    [Fact]
    public void AddSystemTextJson_Options1()
    {
        MockSetupRegister<IGraphQLSerializer, SystemTextJson.GraphQLSerializer>(ServiceLifetime.Singleton, true);
        MockSetupRegister<IGraphQLTextSerializer, SystemTextJson.GraphQLSerializer>(ServiceLifetime.Singleton, true);
        var options = MockSetupConfigure1<System.Text.Json.JsonSerializerOptions>();
        _builder.AddSystemTextJson(options);
        Verify();
    }

    [Fact]
    public void AddSystemTextJson_Options2()
    {
        MockSetupRegister<IGraphQLSerializer, SystemTextJson.GraphQLSerializer>(ServiceLifetime.Singleton, true);
        MockSetupRegister<IGraphQLTextSerializer, SystemTextJson.GraphQLSerializer>(ServiceLifetime.Singleton, true);
        var options = MockSetupConfigure2<System.Text.Json.JsonSerializerOptions>();
        _builder.AddSystemTextJson(options);
        Verify();
    }
    #endregion

    #region - GraphQL.NewtonsoftJson: AddNewtonsoftJson -
    [Fact]
    public void AddNewtonsoftJson()
    {
        MockSetupRegister<IGraphQLSerializer, NewtonsoftJson.GraphQLSerializer>(ServiceLifetime.Singleton, true);
        MockSetupRegister<IGraphQLTextSerializer, NewtonsoftJson.GraphQLSerializer>(ServiceLifetime.Singleton, true);
        MockSetupConfigureNull<JsonSerializerSettings>();
        _builder.AddNewtonsoftJson();
        Verify();
    }

    [Fact]
    public void AddNewtonsoftJson_Options1()
    {
        MockSetupRegister<IGraphQLSerializer, NewtonsoftJson.GraphQLSerializer>(ServiceLifetime.Singleton, true);
        MockSetupRegister<IGraphQLTextSerializer, NewtonsoftJson.GraphQLSerializer>(ServiceLifetime.Singleton, true);
        var options = MockSetupConfigure1<JsonSerializerSettings>();
        _builder.AddNewtonsoftJson(options);
        Verify();
    }

    [Fact]
    public void AddNewtonsoftJson_Options2()
    {
        MockSetupRegister<IGraphQLSerializer, NewtonsoftJson.GraphQLSerializer>(ServiceLifetime.Singleton, true);
        MockSetupRegister<IGraphQLTextSerializer, NewtonsoftJson.GraphQLSerializer>(ServiceLifetime.Singleton, true);
        var options = MockSetupConfigure2<JsonSerializerSettings>();
        _builder.AddNewtonsoftJson(options);
        Verify();
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

    private class TestExecutionStrategySelector : DefaultExecutionStrategySelector
    {
    }

    private class TestExecutionStrategy : ParallelExecutionStrategy
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

    [DoNotRegister]
    private class MyGraphNotRegistered : ObjectGraphType
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
        public ValueTask<object> ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next)
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
        public ValueTask<INodeVisitor> ValidateAsync(ValidationContext context) => throw new NotImplementedException();
    }
}
