using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Types;

public class BuildSourceExpressionTests
{
    [Fact]
    public void BuildSourceExpression_ContextSource_WithValidSource_ReturnsSource()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestClass>(InstanceSource.ContextSource);
        var compiled = expression.Compile();

        var testObj = new TestClass { Value = "test" };
        var context = new ResolveFieldContext { Source = testObj };

        var result = compiled(context);
        result.ShouldBe(testObj);
        result.Value.ShouldBe("test");
    }

    [Fact]
    public void BuildSourceExpression_ContextSource_WithNullSource_ThrowsException()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestClass>(InstanceSource.ContextSource);
        var compiled = expression.Compile();

        var context = new ResolveFieldContext { Source = null };

        var ex = Should.Throw<InvalidOperationException>(() => compiled(context));
        ex.Message.ShouldContain("IResolveFieldContext.Source is null");
    }

    [Fact]
    public void BuildSourceExpression_GetServiceOrCreateInstance_WithServiceRegistered_ReturnsService()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestClass>(InstanceSource.GetServiceOrCreateInstance);
        var compiled = expression.Compile();

        var services = new ServiceCollection();
        var testObj = new TestClass { Value = "from-di" };
        services.AddSingleton(testObj);
        var serviceProvider = services.BuildServiceProvider();

        var context = new ResolveFieldContext { RequestServices = serviceProvider };

        var result = compiled(context);
        result.ShouldBe(testObj);
        result.Value.ShouldBe("from-di");
    }

    [Fact]
    public void BuildSourceExpression_GetServiceOrCreateInstance_WithNoServiceAndNoConstructor_ThrowsException()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestClassMultipleConstructors>(InstanceSource.GetServiceOrCreateInstance);
        var compiled = expression.Compile();

        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var context = new ResolveFieldContext { RequestServices = serviceProvider };

        var ex = Should.Throw<InvalidOperationException>(() => compiled(context));
        ex.Message.ShouldContain("Unable to create instance");
        ex.Message.ShouldContain("no single public constructor found");
    }

    [Fact]
    public void BuildSourceExpression_GetServiceOrCreateInstance_WithNoServiceAndParameterlessConstructor_CreatesNewInstance()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestClass>(InstanceSource.GetServiceOrCreateInstance);
        var compiled = expression.Compile();

        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var context = new ResolveFieldContext { RequestServices = serviceProvider };

        var result = compiled(context);
        result.ShouldNotBeNull();
        result.Value.ShouldBe("default");
    }

    [Fact]
    public void BuildSourceExpression_GetServiceOrCreateInstance_WithNoServiceAndConstructorInjection_CreatesWithDependencies()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestClassWithDependency>(InstanceSource.GetServiceOrCreateInstance);
        var compiled = expression.Compile();

        var services = new ServiceCollection();
        services.AddSingleton("injected-dependency");
        var serviceProvider = services.BuildServiceProvider();

        var context = new ResolveFieldContext { RequestServices = serviceProvider };

        var result = compiled(context);
        result.ShouldNotBeNull();
        result.Dependency.ShouldBe("injected-dependency");
    }

    [Fact]
    public void BuildSourceExpression_GetRequiredService_WithServiceRegistered_ReturnsService()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestClass>(InstanceSource.GetRequiredService);
        var compiled = expression.Compile();

        var services = new ServiceCollection();
        var testObj = new TestClass { Value = "from-di" };
        services.AddSingleton(testObj);
        var serviceProvider = services.BuildServiceProvider();

        var context = new ResolveFieldContext { RequestServices = serviceProvider };

        var result = compiled(context);
        result.ShouldBe(testObj);
        result.Value.ShouldBe("from-di");
    }

    [Fact]
    public void BuildSourceExpression_GetRequiredService_WithNoService_ThrowsException()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestClass>(InstanceSource.GetRequiredService);
        var compiled = expression.Compile();

        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var context = new ResolveFieldContext { RequestServices = serviceProvider };

        var ex = Should.Throw<InvalidOperationException>(() => compiled(context));
        ex.Message.ShouldBe("No service for type 'TestClass' has been registered.");
    }

    [Fact]
    public void BuildSourceExpression_GetRequiredService_WithNullRequestServices_ThrowsMissingRequestServicesException()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestClass>(InstanceSource.GetRequiredService);
        var compiled = expression.Compile();

        var context = new ResolveFieldContext { RequestServices = null };

        Should.Throw<MissingRequestServicesException>(() => compiled(context));
    }

    [Fact]
    public void BuildSourceExpression_NewInstance_WithParameterlessConstructor_CreatesNewInstance()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestClass>(InstanceSource.NewInstance);
        var compiled = expression.Compile();

        var context = new ResolveFieldContext();

        var result = compiled(context);
        result.ShouldNotBeNull();
        result.Value.ShouldBe("default");
    }

    [Fact]
    public void BuildSourceExpression_NewInstance_WithValueType_ReturnsDefault()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestStruct>(InstanceSource.NewInstance);
        var compiled = expression.Compile();

        var context = new ResolveFieldContext();

        var result = compiled(context);
        result.Value.ShouldBe(0);
    }

    [Fact]
    public void BuildSourceExpression_NewInstance_WithConstructorInjection_InjectsDependencies()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestClassWithDependency>(InstanceSource.NewInstance);
        var compiled = expression.Compile();

        var services = new ServiceCollection();
        services.AddSingleton("injected");
        var serviceProvider = services.BuildServiceProvider();

        var context = new ResolveFieldContext { RequestServices = serviceProvider };

        var result = compiled(context);
        result.ShouldNotBeNull();
        result.Dependency.ShouldBe("injected");
    }

    [Fact]
    public void BuildSourceExpression_NewInstance_WithNullableParameter_AllowsNullDependency()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestClassWithNullableDependency>(InstanceSource.NewInstance);
        var compiled = expression.Compile();

        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var context = new ResolveFieldContext { RequestServices = serviceProvider };

        var result = compiled(context);
        result.ShouldNotBeNull();
        result.Dependency.ShouldBeNull();
    }

    [Theory]
    [InlineData(InstanceSource.ContextSource)]
    [InlineData(InstanceSource.GetServiceOrCreateInstance)]
    [InlineData(InstanceSource.GetRequiredService)]
    [InlineData(InstanceSource.NewInstance)]
    public void BuildSourceExpression_WithValidInstanceSource_BuildsExpression(InstanceSource instanceSource)
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestClass>(instanceSource);

        expression.ShouldNotBeNull();

        // Compile to ensure it's valid
        var compiled = expression.Compile();
        compiled.ShouldNotBeNull();
    }

    [Fact]
    public void BuildSourceExpression_WithInvalidInstanceSource_ThrowsException()
    {
        var invalidSource = (InstanceSource)999;

        var ex = Should.Throw<InvalidOperationException>(() =>
            AutoRegisteringOutputHelper.BuildSourceExpression<TestClass>(invalidSource));

        ex.Message.ShouldContain("Unknown instance source");
    }

    [Fact]
    public void BuildSourceExpression_WithoutParameter_UsesAttributeFromType()
    {
        // Test the parameterless overload that reads the attribute
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestClassWithContextSourceAttribute>();

        expression.ShouldNotBeNull();

        var compiled = expression.Compile();
        var testObj = new TestClassWithContextSourceAttribute { Value = "test" };
        var context = new ResolveFieldContext { Source = testObj };

        var result = compiled(context);
        result.ShouldBe(testObj);
    }

    [Fact]
    public void BuildSourceExpression_GetServiceOrCreateInstance_WithRequiredProperty_InjectsDependency()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestClassWithRequiredProperty>(InstanceSource.GetServiceOrCreateInstance);
        var compiled = expression.Compile();

        var services = new ServiceCollection();
        services.AddSingleton("required-dependency");
        var serviceProvider = services.BuildServiceProvider();

        var context = new ResolveFieldContext { RequestServices = serviceProvider };

        var result = compiled(context);
        result.ShouldNotBeNull();
        result.RequiredDependency.ShouldBe("required-dependency");
    }

    [Fact]
    public void BuildSourceExpression_GetServiceOrCreateInstance_WithRequiredField_InjectsDependency()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestClassWithRequiredField>(InstanceSource.GetServiceOrCreateInstance);
        var compiled = expression.Compile();

        var services = new ServiceCollection();
        services.AddSingleton("required-field-dependency");
        var serviceProvider = services.BuildServiceProvider();

        var context = new ResolveFieldContext { RequestServices = serviceProvider };

        var result = compiled(context);
        result.ShouldNotBeNull();
        result.RequiredFieldDependency.ShouldBe("required-field-dependency");
    }

    [Fact]
    public void BuildSourceExpression_GetServiceOrCreateInstance_WithRequiredIResolveFieldContext_InjectsContext()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestClassWithRequiredContext>(InstanceSource.GetServiceOrCreateInstance);
        var compiled = expression.Compile();

        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var context = new ResolveFieldContext { RequestServices = serviceProvider };

        var result = compiled(context);
        result.ShouldNotBeNull();
        result.RequiredContext.ShouldBe(context);
    }

    [Fact]
    public void BuildSourceExpression_GetServiceOrCreateInstance_WithRequiredIServiceProvider_InjectsServiceProvider()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestClassWithRequiredServiceProvider>(InstanceSource.GetServiceOrCreateInstance);
        var compiled = expression.Compile();

        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var context = new ResolveFieldContext { RequestServices = serviceProvider };

        var result = compiled(context);
        result.ShouldNotBeNull();
        result.RequiredServiceProvider.ShouldBe(serviceProvider);
    }

    [Fact]
    public void BuildSourceExpression_GetServiceOrCreateInstance_WithRequiredPropertyOnBaseClass_InjectsDependency()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestDerivedClassWithRequiredOnBase>(InstanceSource.GetServiceOrCreateInstance);
        var compiled = expression.Compile();

        var services = new ServiceCollection();
        services.AddSingleton("base-dependency");
        var serviceProvider = services.BuildServiceProvider();

        var context = new ResolveFieldContext { RequestServices = serviceProvider };

        var result = compiled(context);
        result.ShouldNotBeNull();
        result.BaseRequiredProperty.ShouldBe("base-dependency");
    }

    [Fact]
    public void BuildSourceExpression_GetServiceOrCreateInstance_WithOverriddenRequiredProperty_InjectsDependency()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestDerivedClassWithOverride>(InstanceSource.GetServiceOrCreateInstance);
        var compiled = expression.Compile();

        var services = new ServiceCollection();
        services.AddSingleton("overridden-dependency");
        var serviceProvider = services.BuildServiceProvider();

        var context = new ResolveFieldContext { RequestServices = serviceProvider };

        var result = compiled(context);
        result.ShouldNotBeNull();
        result.VirtualRequiredProperty.ShouldBe("overridden-dependency");
    }

    [Fact]
    public void BuildSourceExpression_NewInstance_WithRequiredProperty_InjectsDependency()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestClassWithRequiredProperty>(InstanceSource.NewInstance);
        var compiled = expression.Compile();

        var services = new ServiceCollection();
        services.AddSingleton("required-dependency");
        var serviceProvider = services.BuildServiceProvider();

        var context = new ResolveFieldContext { RequestServices = serviceProvider };

        var result = compiled(context);
        result.ShouldNotBeNull();
        result.RequiredDependency.ShouldBe("required-dependency");
    }

    [Fact]
    public void BuildSourceExpression_NewInstance_WithRequiredField_InjectsDependency()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestClassWithRequiredField>(InstanceSource.NewInstance);
        var compiled = expression.Compile();

        var services = new ServiceCollection();
        services.AddSingleton("required-field-dependency");
        var serviceProvider = services.BuildServiceProvider();

        var context = new ResolveFieldContext { RequestServices = serviceProvider };

        var result = compiled(context);
        result.ShouldNotBeNull();
        result.RequiredFieldDependency.ShouldBe("required-field-dependency");
    }

    [Fact]
    public void BuildSourceExpression_NewInstance_WithRequiredPropertyOnBaseClass_InjectsDependency()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestDerivedClassWithRequiredOnBase>(InstanceSource.NewInstance);
        var compiled = expression.Compile();

        var services = new ServiceCollection();
        services.AddSingleton("base-dependency");
        var serviceProvider = services.BuildServiceProvider();

        var context = new ResolveFieldContext { RequestServices = serviceProvider };

        var result = compiled(context);
        result.ShouldNotBeNull();
        result.BaseRequiredProperty.ShouldBe("base-dependency");
    }

    [Fact]
    public void BuildSourceExpression_NewInstance_WithStructRequiredProperty_InjectsDependency()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestStructWithRequiredProperty>(InstanceSource.NewInstance);
        var compiled = expression.Compile();

        var services = new ServiceCollection();
        services.AddSingleton("struct-required-dependency");
        var serviceProvider = services.BuildServiceProvider();

        var context = new ResolveFieldContext { RequestServices = serviceProvider };

        var result = compiled(context);
        result.RequiredDependency.ShouldBe("struct-required-dependency");
    }

    [Fact]
    public void BuildSourceExpression_NewInstance_WithStructConstructor_InjectsDependencies()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestStructWithConstructor>(InstanceSource.NewInstance);
        var compiled = expression.Compile();

        var services = new ServiceCollection();
        services.AddSingleton("ctor-dependency");
        var serviceProvider = services.BuildServiceProvider();

        var context = new ResolveFieldContext { RequestServices = serviceProvider };

        var result = compiled(context);
        result.Dependency.ShouldBe("ctor-dependency");
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void BuildSourceExpression_NewInstance_WithStructConstructorAndRequiredMembers_InjectsBoth()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestStructWithConstructorAndRequired>(InstanceSource.NewInstance);
        var compiled = expression.Compile();

        var services = new ServiceCollection();
        services.AddSingleton("ctor-dep");
        services.AddSingleton(new IntWrapper { Value = 123 });
        var serviceProvider = services.BuildServiceProvider();

        var context = new ResolveFieldContext { RequestServices = serviceProvider };

        var result = compiled(context);
        result.ConstructorDependency.ShouldBe("ctor-dep");
        result.RequiredPropertyDependency.Value.ShouldBe(123);
    }

    [Fact]
    public void BuildSourceExpression_GetServiceOrCreateInstance_WithStructRequiredProperty_InjectsDependency()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestStructWithRequiredProperty>(InstanceSource.GetServiceOrCreateInstance);
        var compiled = expression.Compile();

        var services = new ServiceCollection();
        services.AddSingleton("struct-required-dependency");
        var serviceProvider = services.BuildServiceProvider();

        var context = new ResolveFieldContext { RequestServices = serviceProvider };

        var result = compiled(context);
        result.RequiredDependency.ShouldBe("struct-required-dependency");
    }

    [Fact]
    public void BuildSourceExpression_GetServiceOrCreateInstance_WithStructConstructor_InjectsDependencies()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestStructWithConstructor>(InstanceSource.GetServiceOrCreateInstance);
        var compiled = expression.Compile();

        var services = new ServiceCollection();
        services.AddSingleton("ctor-dependency");
        var serviceProvider = services.BuildServiceProvider();

        var context = new ResolveFieldContext { RequestServices = serviceProvider };

        var result = compiled(context);
        result.Dependency.ShouldBe("ctor-dependency");
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void BuildSourceExpression_GetServiceOrCreateInstance_WithStructConstructorAndRequiredMembers_InjectsBoth()
    {
        var expression = AutoRegisteringOutputHelper.BuildSourceExpression<TestStructWithConstructorAndRequired>(InstanceSource.GetServiceOrCreateInstance);
        var compiled = expression.Compile();

        var services = new ServiceCollection();
        services.AddSingleton("ctor-dep");
        services.AddSingleton(new IntWrapper { Value = 123 });
        var serviceProvider = services.BuildServiceProvider();

        var context = new ResolveFieldContext { RequestServices = serviceProvider };

        var result = compiled(context);
        result.ConstructorDependency.ShouldBe("ctor-dep");
        result.RequiredPropertyDependency.Value.ShouldBe(123);
    }

    // Test classes
    private class TestClass
    {
        public string Value { get; set; } = "default";
    }

    private class TestClassWithDependency
    {
        public TestClassWithDependency(string dependency)
        {
            Dependency = dependency;
        }

        public string Dependency { get; }
    }

    private class TestClassWithNullableDependency
    {
        public TestClassWithNullableDependency(string? dependency)
        {
            Dependency = dependency;
        }

        public string? Dependency { get; }
    }

    private class TestClassMultipleConstructors
    {
        public TestClassMultipleConstructors()
        {
        }

        public TestClassMultipleConstructors(string value)
        {
            _ = value;
        }
    }

    [InstanceSource(InstanceSource.ContextSource)]
    private class TestClassWithContextSourceAttribute
    {
        public string Value { get; set; } = "default";
    }

    private struct TestStruct
    {
        public int Value { get; set; }
    }

    private class TestClassWithRequiredProperty
    {
        public required string RequiredDependency { get; set; }
    }

    private class TestClassWithRequiredField
    {
        public required string RequiredFieldDependency = null!;
    }

    private class TestClassWithRequiredContext
    {
        public required IResolveFieldContext RequiredContext { get; set; }
    }

    private class TestClassWithRequiredServiceProvider
    {
        public required IServiceProvider RequiredServiceProvider { get; set; }
    }

    private class TestBaseClassWithRequiredProperty
    {
        public required string BaseRequiredProperty { get; set; }
    }

    private class TestDerivedClassWithRequiredOnBase : TestBaseClassWithRequiredProperty
    {
        public string DerivedProperty { get; set; } = "derived";
    }

    private class TestBaseClassWithVirtualRequired
    {
        public virtual required string VirtualRequiredProperty { get; set; }
    }

    private class TestDerivedClassWithOverride : TestBaseClassWithVirtualRequired
    {
        public override required string VirtualRequiredProperty { get; set; }
    }

    private struct TestStructWithRequiredProperty
    {
        public required string RequiredDependency { get; set; }
        public int Value { get; set; }
    }

    private struct TestStructWithConstructor
    {
        public TestStructWithConstructor(string dependency)
        {
            Dependency = dependency;
            Value = 42;
        }

        public string Dependency { get; }
        public int Value { get; }
    }

    private struct TestStructWithConstructorAndRequired
    {
        public TestStructWithConstructorAndRequired(string constructorDependency)
        {
            ConstructorDependency = constructorDependency;
        }

        public string ConstructorDependency { get; }
        public required IntWrapper RequiredPropertyDependency { get; set; }
    }

    // Wrapper for int to work with DI (value types can't be registered with AddSingleton)
    private class IntWrapper
    {
        public int Value { get; set; }
    }
}
