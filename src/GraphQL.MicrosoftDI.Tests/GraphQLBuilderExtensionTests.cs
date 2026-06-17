using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI.Tests;

public class GraphQLBuilderExtensionTests
{
    [Fact]
    public void AddGraphQL()
    {
        bool executed = false;
        var services = new ServiceCollection();
        services.AddGraphQL(builder =>
        {
            builder.ShouldBeOfType<GraphQLBuilder>();
            executed = true;
        });
        executed.ShouldBeTrue();
        services.BuildServiceProvider().GetService<IDocumentExecuter>().ShouldNotBeNull();
    }

    [Theory]
    [InlineData(DI.ServiceLifetime.Singleton)]
    [InlineData(DI.ServiceLifetime.Scoped)]
    public void AddSelfActivatingSchema(DI.ServiceLifetime serviceLifetime)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Class2.Instance);
        services.AddGraphQL(b => b.AddSelfActivatingSchema<MySchema>(serviceLifetime));
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
        using var serviceProvider = services.BuildServiceProvider();
        var schema1 = serviceProvider.GetRequiredService<MySchema>();
        schema1.ShouldBeOfType<MySchema>();
        var schema2 = serviceProvider.GetRequiredService<ISchema>();
        schema2.ShouldBeOfType<MySchema>();
        ReferenceEquals(schema1, schema2).ShouldBeTrue();
    }

    [Fact]
    public void AddSelfActivatingSchema_Transient()
    {
        var services = new ServiceCollection();
        services.AddGraphQL(builder => Should.Throw<InvalidOperationException>(() => builder.AddSelfActivatingSchema<MySchema>(DI.ServiceLifetime.Transient)));
    }

    [Fact]
    public void VerifyServices_PassesWhenAllServicesRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<TestService1>();
        services.AddSingleton<TestService2>();

        // Act
        services.AddGraphQL(builder => builder
            .AddSchema<VerifyServicesTestSchema>()
            .ValidateServices());

        var serviceProvider = services.BuildServiceProvider();
        var schema = serviceProvider.GetRequiredService<ISchema>();

        // Assert - should not throw
        schema.Initialize();
    }

    [Fact]
    public void VerifyServices_ThrowsWhenOnlyOneServiceRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<TestService1>(); // Only register TestService1, not TestService2

        // Act & Assert
        services.AddGraphQL(builder => builder
            .AddSchema<VerifyServicesTestSchema>()
            .ValidateServices());

        var serviceProvider = services.BuildServiceProvider();

        // This should throw because TestService2 is not registered
        Should.Throw<InvalidOperationException>(() =>
        {
            var schema = serviceProvider.GetRequiredService<ISchema>();
            schema.Initialize();
        }).Message.ShouldBe("""
            The service 'GraphQL.MicrosoftDI.Tests.GraphQLBuilderExtensionTests+TestService2' required by 'Class3.getData' is not registered.
            """, StringCompareShould.IgnoreLineEndings);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void VerifyServices_ThrowsWhenBothServicesAreMissing(bool enable)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        services.AddGraphQL(builder => builder
            .AddSchema<VerifyServicesTestSchema>()
            .ValidateServices(enable));

        var serviceProvider = services.BuildServiceProvider();

        if (enable)
        {
            // This should throw because TestService is not registered
            Should.Throw<InvalidOperationException>(() =>
            {
                var schema = serviceProvider.GetRequiredService<ISchema>();
                schema.Initialize();
            }).Message.ShouldBe("""
            The following service validation errors were found:
            The service 'GraphQL.MicrosoftDI.Tests.GraphQLBuilderExtensionTests+TestService1' required by 'Class3.getData' is not registered.
            The service 'GraphQL.MicrosoftDI.Tests.GraphQLBuilderExtensionTests+TestService2' required by 'Class3.getData' is not registered.
            """, StringCompareShould.IgnoreLineEndings);
        }
        else
        {
            // This should not throw because service validation is disabled
            var schema = serviceProvider.GetRequiredService<ISchema>();
            schema.Initialize();
        }
    }

    [Fact]
    public void VerifyServices_RegularObjectGraphType_PassesWhenAllServicesRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        // Register all 15 services
        services.AddSingleton<Service1>();
        services.AddSingleton<Service2>();
        services.AddSingleton<Service3>();
        services.AddSingleton<Service4>();
        services.AddSingleton<Service5>();
        services.AddSingleton<Service6>();
        services.AddSingleton<Service7>();
        services.AddSingleton<Service8>();
        services.AddSingleton<Service9>();
        services.AddSingleton<Service10>();
        services.AddSingleton<Service11>();
        services.AddSingleton<Service12>();
        services.AddSingleton<Service13>();
        services.AddSingleton<Service14>();
        services.AddSingleton<Service15>();
        services.AddSingleton<Service16>();

        // Act
        services.AddGraphQL(builder => builder
            .AddSchema<RegularObjectGraphTypeSchema>()
            .ValidateServices());

        var serviceProvider = services.BuildServiceProvider();
        var schema = serviceProvider.GetRequiredService<ISchema>();

        // Assert - should not throw
        schema.Initialize();
    }

    [Fact]
    public void VerifyServices_RegularObjectGraphType_ThrowsWhenAllServicesMissing()
    {
        // Arrange
        var services = new ServiceCollection();
        // Don't register any services

        // Act & Assert
        services.AddGraphQL(builder => builder
            .AddSchema<RegularObjectGraphTypeSchema>()
            .ValidateServices());

        var serviceProvider = services.BuildServiceProvider();

        // This should throw because all services are missing
        var exception = Should.Throw<InvalidOperationException>(() =>
        {
            var schema = serviceProvider.GetRequiredService<ISchema>();
            schema.Initialize();
        });

        // Verify that the error message contains all 15 missing services
        exception.Message.ShouldContain("The following service validation errors were found:");
        for (int i = 1; i <= 16; i++)
        {
            exception.Message.ShouldContain($"The service 'GraphQL.MicrosoftDI.Tests.GraphQLBuilderExtensionTests+Service{i}' required by ");
        }
    }

    [Fact]
    public void VerifyServices_AsyncRegularObjectGraphType_PassesWhenAllServicesRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        // Register all 15 services
        services.AddSingleton<Service1>();
        services.AddSingleton<Service2>();
        services.AddSingleton<Service3>();
        services.AddSingleton<Service4>();
        services.AddSingleton<Service5>();
        services.AddSingleton<Service6>();
        services.AddSingleton<Service7>();
        services.AddSingleton<Service8>();
        services.AddSingleton<Service9>();
        services.AddSingleton<Service10>();
        services.AddSingleton<Service11>();
        services.AddSingleton<Service12>();
        services.AddSingleton<Service13>();
        services.AddSingleton<Service14>();
        services.AddSingleton<Service15>();

        // Act
        services.AddGraphQL(builder => builder
            .AddSchema<AsyncRegularObjectGraphTypeSchema>()
            .ValidateServices());

        var serviceProvider = services.BuildServiceProvider();
        var schema = serviceProvider.GetRequiredService<ISchema>();

        // Assert - should not throw
        schema.Initialize();
    }

    [Fact]
    public void VerifyServices_AsyncRegularObjectGraphType_ThrowsWhenAllServicesMissing()
    {
        // Arrange
        var services = new ServiceCollection();
        // Don't register any services

        // Act & Assert
        services.AddGraphQL(builder => builder
            .AddSchema<AsyncRegularObjectGraphTypeSchema>()
            .ValidateServices());

        var serviceProvider = services.BuildServiceProvider();

        // This should throw because all services are missing
        var exception = Should.Throw<InvalidOperationException>(() =>
        {
            var schema = serviceProvider.GetRequiredService<ISchema>();
            schema.Initialize();
        });

        // Verify that the error message contains all 15 missing services
        exception.Message.ShouldContain("The following service validation errors were found:");
        for (int i = 1; i <= 15; i++)
        {
            exception.Message.ShouldContain($"The service 'GraphQL.MicrosoftDI.Tests.GraphQLBuilderExtensionTests+Service{i}' required by ");
        }
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
        public static Class2 Instance = new();

        private Class2()
        {
        }
    }

    private class TestService1
    {
        public string GetData1() => "Test Data 1";
    }

    private class TestService2
    {
        public string GetData2() => "Test Data 2";
    }

    private class VerifyServicesTestSchema : Schema
    {
        public VerifyServicesTestSchema(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Query = new AutoRegisteringObjectGraphType<Class3>();
        }
    }

    private class Class3
    {
        public string GetData(string input, [FromServices] TestService1 service1, [FromServices] TestService2 service2)
        {
            return service1.GetData1() + " & " + service2.GetData2() + ": " + input;
        }
    }

    // 15 different service classes for the new tests
    private class Service1 { }
    private class Service2 { }
    private class Service3 { }
    private class Service4 { }
    private class Service5 { }
    private class Service6 { }
    private class Service7 { }
    private class Service8 { }
    private class Service9 { }
    private class Service10 { }
    private class Service11 { }
    private class Service12 { }
    private class Service13 { }
    private class Service14 { }
    private class Service15 { }
    private class Service16 { }

    // Regular ObjectGraphType for the query
    private class RegularQueryType : ObjectGraphType
    {
        public RegularQueryType()
        {
            Name = "Query";

            // Field with 1 service
            Field<StringGraphType>("field1")
                .Resolve()
                .WithService<Service1>()
                .Resolve((context, service1) => "");

            // Field with 2 services
            Field<StringGraphType>("field2")
                .Resolve()
                .WithService<Service2>()
                .WithService<Service3>()
                .Resolve((context, service2, service3) => "");

            // Field with 3 services
            Field<StringGraphType>("field3")
                .Resolve()
                .WithService<Service4>()
                .WithService<Service5>()
                .WithService<Service6>()
                .Resolve((context, service4, service5, service6) => "");

            // Field with 4 services
            Field<StringGraphType>("field4")
                .Resolve()
                .WithService<Service7>()
                .WithService<Service8>()
                .WithService<Service9>()
                .WithService<Service10>()
                .Resolve((context, service7, service8, service9, service10) => "");

            // Field with 5 services
            Field<StringGraphType>("field5")
                .Resolve()
                .WithService<Service11>()
                .WithService<Service12>()
                .WithService<Service13>()
                .WithService<Service14>()
                .WithService<Service15>()
                .Resolve((context, service11, service12, service13, service14, service15) => "");

            Field<StringGraphType>("field6")
                .DependsOn<Service16>()
                .Resolve((context) => "");
        }
    }

    // Schema using the regular ObjectGraphType
    private class RegularObjectGraphTypeSchema : Schema
    {
        public RegularObjectGraphTypeSchema(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Query = new RegularQueryType();
        }
    }

    // Regular ObjectGraphType for the query with async resolvers
    private class AsyncRegularQueryType : ObjectGraphType
    {
        public AsyncRegularQueryType()
        {
            Name = "Query";

            // Field with 1 service
            Field<StringGraphType>("field1")
                .Resolve()
                .WithService<Service1>()
                .ResolveAsync((context, service1) => Task.FromResult<object?>(null));

            // Field with 2 services
            Field<StringGraphType>("field2")
                .Resolve()
                .WithService<Service2>()
                .WithService<Service3>()
                .ResolveAsync((context, service2, service3) => Task.FromResult<object?>(null));

            // Field with 3 services
            Field<StringGraphType>("field3")
                .Resolve()
                .WithService<Service4>()
                .WithService<Service5>()
                .WithService<Service6>()
                .ResolveAsync((context, service4, service5, service6) => Task.FromResult<object?>(null));

            // Field with 4 services
            Field<StringGraphType>("field4")
                .Resolve()
                .WithService<Service7>()
                .WithService<Service8>()
                .WithService<Service9>()
                .WithService<Service10>()
                .ResolveAsync((context, service7, service8, service9, service10) => Task.FromResult<object?>(null));

            // Field with 5 services
            Field<StringGraphType>("field5")
                .Resolve()
                .WithService<Service11>()
                .WithService<Service12>()
                .WithService<Service13>()
                .WithService<Service14>()
                .WithService<Service15>()
                .ResolveAsync((context, service11, service12, service13, service14, service15) => Task.FromResult<object?>(null));
        }
    }

    // Schema using the regular ObjectGraphType with async resolvers
    private class AsyncRegularObjectGraphTypeSchema : Schema
    {
        public AsyncRegularObjectGraphTypeSchema(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Query = new AsyncRegularQueryType();
        }
    }
}
