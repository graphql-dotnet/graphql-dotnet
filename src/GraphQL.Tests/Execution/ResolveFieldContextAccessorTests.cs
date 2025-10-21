using GraphQL.Execution;
using GraphQL.MicrosoftDI;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Execution;

public class ResolveFieldContextAccessorTests
{
    [Fact]
    public async Task ContextAccessor_ReturnsCorrectContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddSchema<TestSchema>()
            .AddResolveFieldContextAccessor());

        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        var accessor = provider.GetRequiredService<IResolveFieldContextAccessor>();

        // Act
        var result = await schema.ExecuteAsync(_ =>
        {
            _.Query = "{ testField }";
            _.RequestServices = provider;
        });

        // Assert
        result.ShouldBeCrossPlatJson("""
            {
              "data": {
                "testField": "match"
              }
            }
            """);

        // Context should be null after execution completes
        Should.Throw<InvalidOperationException>(() =>
        {
            var _ = accessor.Context;
        });
    }

    [Fact]
    public async Task ContextAccessor_WithScopedResolver_ReturnsCorrectContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<TestService>();
        services.AddGraphQL(b => b
            .AddSchema<TestScopedSchema>()
            .AddResolveFieldContextAccessor());

        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();

        // Act
        var result = await schema.ExecuteAsync(_ =>
        {
            _.Query = "{ scopedField }";
            _.RequestServices = provider;
        });

        // Assert
        result.ShouldBeCrossPlatJson("""
            {
              "data": {
                "scopedField": "match"
              }
            }
            """);
    }

    [Fact]
    public async Task ContextAccessor_MultipleFields_ReturnsCorrectContextForEach()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddSchema<TestMultiFieldSchema>()
            .AddResolveFieldContextAccessor());

        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();

        // Act
        var result = await schema.ExecuteAsync(_ =>
        {
            _.Query = "{ field1 field2 field3 }";
            _.RequestServices = provider;
        });

        // Assert
        result.ShouldBeCrossPlatJson("""
            {
              "data": {
                "field1": "match",
                "field2": "match",
                "field3": "match"
              }
            }
            """);
    }

    [Fact]
    public async Task ContextAccessor_WithoutConfiguration_ReturnsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddSchema<TestSchema>());

        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();

        // Act
        var result = await schema.ExecuteAsync(_ =>
        {
            _.Query = "{ testField }";
            _.RequestServices = provider;
        });

        // Assert - should work fine without accessor configured
        result.ShouldBeCrossPlatJson("""
            {
              "data": {
                "testField": "no match"
              }
            }
            """);
    }

    [Fact]
    public void ContextAccessor_OutsideExecution_ReturnsNull()
    {
        // Arrange
        var accessor = ResolveFieldContextAccessor.Instance;

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
        {
            var _ = accessor.Context;
        });
    }

    [Fact]
    public async Task ContextAccessor_TypedFunctionFields_WontReturnCorrectContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddSelfActivatingSchema<TestNestedSchema>()
            .AddResolveFieldContextAccessor());

        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();

        // Act
        var result = await schema.ExecuteAsync(_ =>
        {
            _.Query = "{ parent { child } }";
            _.RequestServices = provider;
        });

        // Assert -- fails since FuncFieldResolver<T> uses a ResolveFieldContextAdapter for type casting
        result.ShouldBeCrossPlatJson("""
            {
              "data": {
                "parent": {
                  "child": "no match"
                }
              }
            }
            """);
    }

    private class TestSchema : Schema
    {
        public TestSchema(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Query = new TestQuery(serviceProvider);
        }
    }

    private class TestQuery : ObjectGraphType
    {
        public TestQuery(IServiceProvider serviceProvider)
        {
            Field<StringGraphType>("testField")
                .Resolve(context =>
                {
                    var accessor = serviceProvider.GetService<IResolveFieldContextAccessor>();
                    return accessor?.Context == context ? "match" : "no match";
                });
        }
    }

    private class TestScopedSchema : Schema
    {
        public TestScopedSchema(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Query = new TestScopedQuery();
        }
    }

    private class TestScopedQuery : ObjectGraphType
    {
        public TestScopedQuery()
        {
            Field<StringGraphType>("scopedField")
                .Resolve()
                .WithScope()
                .WithService<TestService>()
                .ResolveAsync(async (context, service) =>
                {
                    await Task.CompletedTask;
                    var currentContext = service.GetCurrentContext();
                    return currentContext == context ? "match" : "no match";
                });
        }
    }

    private class TestService
    {
        private readonly IResolveFieldContextAccessor _accessor;

        public TestService(IResolveFieldContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public IResolveFieldContext? GetCurrentContext() => _accessor.Context;
    }

    private class TestMultiFieldSchema : Schema
    {
        public TestMultiFieldSchema(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Query = new TestMultiFieldQuery(serviceProvider);
        }
    }

    private class TestMultiFieldQuery : ObjectGraphType
    {
        public TestMultiFieldQuery(IServiceProvider serviceProvider)
        {
            Field<StringGraphType>("field1")
                .Resolve(context =>
                {
                    var accessor = serviceProvider.GetService<IResolveFieldContextAccessor>();
                    return accessor?.Context == context ? "match" : "no match";
                });

            Field<StringGraphType>("field2")
                .Resolve(context =>
                {
                    var accessor = serviceProvider.GetService<IResolveFieldContextAccessor>();
                    return accessor?.Context == context ? "match" : "no match";
                });

            Field<StringGraphType>("field3")
                .Resolve(context =>
                {
                    var accessor = serviceProvider.GetService<IResolveFieldContextAccessor>();
                    return accessor?.Context == context ? "match" : "no match";
                });
        }
    }

    private class TestNestedSchema : Schema
    {
        public TestNestedSchema(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Query = new TestNestedQuery(serviceProvider);
        }
    }

    private class TestNestedQuery : ObjectGraphType
    {
        public TestNestedQuery(IServiceProvider serviceProvider)
        {
            Field<ParentType>("parent")
                .Resolve(context =>
                {
                    var accessor = serviceProvider.GetService<IResolveFieldContextAccessor>();
                    accessor.ShouldNotBeNull().Context.ShouldBe(context);
                    return new Parent();
                });
        }
    }

    private class ParentType : ObjectGraphType<Parent>
    {
        public ParentType(IServiceProvider serviceProvider)
        {
            Field<StringGraphType>("child")
                .Resolve(context =>
                {
                    var accessor = serviceProvider.GetService<IResolveFieldContextAccessor>();
                    return accessor?.Context == context ? "match" : "no match";
                });
        }
    }

    private class Parent
    {
    }
}
