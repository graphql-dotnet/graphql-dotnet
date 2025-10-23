using GraphQL.Instrumentation;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Instrumentation;

/// <summary>
/// Tests to verify the order in which schema and field middleware are applied and executed.
/// </summary>
public class MiddlewareOrderTests
{
    [Fact]
    public async Task schema_middleware_executes_before_field_middleware()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(executionOrder);
        services.AddGraphQL(b => b
            .AddSchema<TestSchema>()
            .ConfigureSchema((schema, sp) =>
            {
                var order = sp.GetRequiredService<List<string>>();

                // Add schema-wide middleware in order
                schema.FieldMiddleware.Use(next => context =>
                {
                    order.Add("Schema1-Before");
                    var result = next(context);
                    order.Add("Schema1-After");
                    return result;
                });

                schema.FieldMiddleware.Use(next => context =>
                {
                    order.Add("Schema2-Before");
                    var result = next(context);
                    order.Add("Schema2-After");
                    return result;
                });
            }));

        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();

        // Act
        var result = await schema.ExecuteAsync(_ =>
        {
            _.Query = "{ test }";
            _.RequestServices = provider;
        });

        // Assert
        result.ShouldBeCrossPlatJson("""
            {
              "data": {
                "test": "result"
              }
            }
            """);

        // Verify execution order:
        // Schema middleware should execute in the order it was applied (Schema1, then Schema2)
        // Field middleware should execute in the order it was applied (Field1, then Field2)
        // Schema middleware should wrap field middleware (execute before and after)
        executionOrder.ShouldBe(new[]
        {
            "Schema1-Before",  // First schema middleware starts
            "Schema2-Before",  // Second schema middleware starts
            "Field1-Before",   // First field middleware starts
            "Field2-Before",   // Second field middleware starts
            // Resolver executes here
            "Field2-After",    // Second field middleware ends
            "Field1-After",    // First field middleware ends
            "Schema2-After",   // Second schema middleware ends
            "Schema1-After"    // First schema middleware ends
        });
    }

    [Fact]
    public async Task multiple_schema_middleware_execute_in_order()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(executionOrder);
        services.AddGraphQL(b => b
            .AddSchema<SimpleSchema>()
            .ConfigureSchema((schema, sp) =>
            {
                var order = sp.GetRequiredService<List<string>>();

                schema.FieldMiddleware.Use(next => context =>
                {
                    order.Add("Schema1");
                    return next(context);
                });

                schema.FieldMiddleware.Use(next => context =>
                {
                    order.Add("Schema2");
                    return next(context);
                });

                schema.FieldMiddleware.Use(next => context =>
                {
                    order.Add("Schema3");
                    return next(context);
                });
            }));

        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();

        // Act
        var result = await schema.ExecuteAsync(_ =>
        {
            _.Query = "{ test }";
            _.RequestServices = provider;
        });

        // Assert
        result.ShouldBeCrossPlatJson("""{"data":{"test":"result"}}""");
        executionOrder.ShouldBe(new[] { "Schema1", "Schema2", "Schema3" });
    }

    [Fact]
    public async Task field_middleware_with_lambda_executes_in_order()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(executionOrder);
        services.AddGraphQL(b => b.AddSchema<FieldMiddlewareLambdaSchema>());

        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();

        // Act
        var result = await schema.ExecuteAsync(_ =>
        {
            _.Query = "{ test }";
            _.RequestServices = provider;
        });

        // Assert
        result.ShouldBeCrossPlatJson("""{"data":{"test":"result"}}""");
        executionOrder.ShouldBe(new[] { "Field1", "Field2", "Field3" });
    }

    [Fact]
    public async Task field_middleware_with_class_instance_executes_in_order()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(executionOrder);
        services.AddGraphQL(b => b.AddSchema<FieldMiddlewareClassSchema>());

        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();

        // Act
        var result = await schema.ExecuteAsync(_ =>
        {
            _.Query = "{ test }";
            _.RequestServices = provider;
        });

        // Assert
        result.ShouldBeCrossPlatJson("""{"data":{"test":"result"}}""");
        executionOrder.ShouldBe(new[] { "Class1", "Class2", "Class3" });
    }

    [Fact]
    public async Task field_middleware_with_di_type_executes_in_order()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(executionOrder);
        services.AddSingleton<OrderTrackingMiddleware1>();
        services.AddSingleton<OrderTrackingMiddleware2>();
        services.AddSingleton<OrderTrackingMiddleware3>();
        services.AddGraphQL(b => b.AddSchema<FieldMiddlewareDISchema>());

        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();

        // Act
        var result = await schema.ExecuteAsync(_ =>
        {
            _.Query = "{ test }";
            _.RequestServices = provider;
        });

        // Assert
        result.ShouldBeCrossPlatJson("""{"data":{"test":"result"}}""");
        executionOrder.ShouldBe(new[] { "DI1", "DI2", "DI3" });
    }

    [Fact]
    public async Task combined_middleware_modify_result_in_correct_order()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddSchema<ModifyResultSchema>()
            .ConfigureSchema((schema, sp) =>
            {
                schema.FieldMiddleware.Use(next => async context =>
                {
                    var result = await next(context);
                    return result + "-S1";
                });

                schema.FieldMiddleware.Use(next => async context =>
                {
                    var result = await next(context);
                    return result + "-S2";
                });
            }));

        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();

        // Act
        var result = await schema.ExecuteAsync(_ =>
        {
            _.Query = "{ test }";
            _.RequestServices = provider;
        });

        // Assert
        result.ShouldBeCrossPlatJson("""{"data":{"test":"R-F2-F1-S2-S1"}}""");
    }

    [Fact]
    public async Task different_fields_have_independent_field_middleware()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(executionOrder);
        services.AddGraphQL(b => b
            .AddSchema<MultiFieldSchema>()
            .ConfigureSchema((schema, sp) =>
            {
                var order = sp.GetRequiredService<List<string>>();
                schema.FieldMiddleware.Use(next => context =>
                {
                    order.Add($"Schema-{context.FieldDefinition.Name}");
                    return next(context);
                });
            }));

        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();

        // Act
        var result = await schema.ExecuteAsync(_ =>
        {
            _.Query = "{ field1 field2 }";
            _.RequestServices = provider;
        });

        // Assert
        result.ShouldBeCrossPlatJson("""{"data":{"field1":"result1","field2":"result2"}}""");
        executionOrder.ShouldContain("Schema-field1");
        executionOrder.ShouldContain("Field1-Middleware");
        executionOrder.ShouldContain("Schema-field2");
        executionOrder.ShouldContain("Field2-Middleware");
    }

    [Fact]
    public async Task all_middleware_types_combined_execute_in_correct_order()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(executionOrder);
        services.AddSingleton<OrderTrackingMiddleware1>();
        services.AddGraphQL(b => b
            .AddSchema<CombinedMiddlewareSchema>()
            .ConfigureSchema((schema, sp) =>
            {
                var order = sp.GetRequiredService<List<string>>();

                schema.FieldMiddleware.Use(next => context =>
                {
                    order.Add("Schema1");
                    return next(context);
                });

                schema.FieldMiddleware.Use(next => context =>
                {
                    order.Add("Schema2");
                    return next(context);
                });
            }));

        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();

        // Act
        var result = await schema.ExecuteAsync(_ =>
        {
            _.Query = "{ test }";
            _.RequestServices = provider;
        });

        // Assert
        result.ShouldBeCrossPlatJson("""{"data":{"test":"result"}}""");
        executionOrder.ShouldBe(new[]
        {
            "Schema1",
            "Schema2",
            "FieldLambda",
            "FieldClass",
            "DI1"
        });
    }

    [Fact]
    public async Task schema_middleware_via_use_middleware_executes_in_order()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(executionOrder);
        services.AddGraphQL(b => b
            .AddSchema<SimpleSchema>()
            .UseMiddleware<OrderTrackingMiddleware1>()
            .UseMiddleware<OrderTrackingMiddleware2>()
            .UseMiddleware<OrderTrackingMiddleware3>());

        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();

        // Act
        var result = await schema.ExecuteAsync(_ =>
        {
            _.Query = "{ test }";
            _.RequestServices = provider;
        });

        // Assert
        result.ShouldBeCrossPlatJson("""{"data":{"test":"result"}}""");
        executionOrder.ShouldBe(new[] { "DI1", "DI2", "DI3" });
    }

    [Fact]
    public async Task schema_middleware_via_use_middleware_with_instance_executes_in_order()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(executionOrder);
        services.AddGraphQL(b => b
            .AddSchema<SimpleSchema>()
            .UseMiddleware(new OrderTrackingMiddleware(executionOrder, "Instance1"))
            .UseMiddleware(new OrderTrackingMiddleware(executionOrder, "Instance2"))
            .UseMiddleware(new OrderTrackingMiddleware(executionOrder, "Instance3")));

        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();

        // Act
        var result = await schema.ExecuteAsync(_ =>
        {
            _.Query = "{ test }";
            _.RequestServices = provider;
        });

        // Assert
        result.ShouldBeCrossPlatJson("""{"data":{"test":"result"}}""");
        executionOrder.ShouldBe(new[] { "Instance1", "Instance2", "Instance3" });
    }

    [Fact]
    public async Task combined_use_middleware_and_configure_schema_execute_in_order()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(executionOrder);
        services.AddGraphQL(b => b
            .AddSchema<SimpleSchema>()
            .UseMiddleware<OrderTrackingMiddleware1>()
            .UseMiddleware<OrderTrackingMiddleware2>()
            .ConfigureSchema((schema, sp) =>
            {
                var order = sp.GetRequiredService<List<string>>();
                schema.FieldMiddleware.Use(next => context =>
                {
                    order.Add("ConfigureSchema");
                    return next(context);
                });
            }));

        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();

        // Act
        var result = await schema.ExecuteAsync(_ =>
        {
            _.Query = "{ test }";
            _.RequestServices = provider;
        });

        // Assert
        result.ShouldBeCrossPlatJson("""{"data":{"test":"result"}}""");
        // UseMiddleware is applied first, then ConfigureSchema
        executionOrder.ShouldBe(new[] { "DI1", "DI2", "ConfigureSchema" });
    }

    [Fact]
    public async Task use_middleware_with_field_middleware_executes_in_correct_order()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(executionOrder);
        services.AddGraphQL(b => b
            .AddSchema<FieldMiddlewareLambdaSchema>()
            .UseMiddleware<OrderTrackingMiddleware1>()
            .UseMiddleware<OrderTrackingMiddleware2>());

        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();

        // Act
        var result = await schema.ExecuteAsync(_ =>
        {
            _.Query = "{ test }";
            _.RequestServices = provider;
        });

        // Assert
        result.ShouldBeCrossPlatJson("""{"data":{"test":"result"}}""");
        // Schema middleware (UseMiddleware) executes before field middleware
        executionOrder.ShouldBe(new[] { "DI1", "DI2", "Field1", "Field2", "Field3" });
    }

    // Test schemas
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
            var executionOrder = serviceProvider.GetRequiredService<List<string>>();

            Field<StringGraphType>("test")
                .Resolve(_ => "result")
                .ApplyMiddleware(next => context =>
                {
                    executionOrder.Add("Field1-Before");
                    var result = next(context);
                    executionOrder.Add("Field1-After");
                    return result;
                })
                .ApplyMiddleware(next => context =>
                {
                    executionOrder.Add("Field2-Before");
                    var result = next(context);
                    executionOrder.Add("Field2-After");
                    return result;
                });
        }
    }

    private class SimpleSchema : Schema
    {
        public SimpleSchema(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Query = new SimpleQuery();
        }
    }

    private class SimpleQuery : ObjectGraphType
    {
        public SimpleQuery()
        {
            Field<StringGraphType>("test").Resolve(_ => "result");
        }
    }

    private class FieldMiddlewareLambdaSchema : Schema
    {
        public FieldMiddlewareLambdaSchema(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Query = new FieldMiddlewareLambdaQuery(serviceProvider);
        }
    }

    private class FieldMiddlewareLambdaQuery : ObjectGraphType
    {
        public FieldMiddlewareLambdaQuery(IServiceProvider serviceProvider)
        {
            var executionOrder = serviceProvider.GetRequiredService<List<string>>();

            Field<StringGraphType>("test")
                .Resolve(_ => "result")
                .ApplyMiddleware(next => context =>
                {
                    executionOrder.Add("Field1");
                    return next(context);
                })
                .ApplyMiddleware(next => context =>
                {
                    executionOrder.Add("Field2");
                    return next(context);
                })
                .ApplyMiddleware(next => context =>
                {
                    executionOrder.Add("Field3");
                    return next(context);
                });
        }
    }

    private class FieldMiddlewareClassSchema : Schema
    {
        public FieldMiddlewareClassSchema(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Query = new FieldMiddlewareClassQuery(serviceProvider);
        }
    }

    private class FieldMiddlewareClassQuery : ObjectGraphType
    {
        public FieldMiddlewareClassQuery(IServiceProvider serviceProvider)
        {
            var executionOrder = serviceProvider.GetRequiredService<List<string>>();

            Field<StringGraphType>("test")
                .Resolve(_ => "result")
                .ApplyMiddleware(new OrderTrackingMiddleware(executionOrder, "Class1"))
                .ApplyMiddleware(new OrderTrackingMiddleware(executionOrder, "Class2"))
                .ApplyMiddleware(new OrderTrackingMiddleware(executionOrder, "Class3"));
        }
    }

    private class FieldMiddlewareDISchema : Schema
    {
        public FieldMiddlewareDISchema(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Query = new FieldMiddlewareDIQuery();
        }
    }

    private class FieldMiddlewareDIQuery : ObjectGraphType
    {
        public FieldMiddlewareDIQuery()
        {
            Field<StringGraphType>("test")
                .Resolve(_ => "result")
                .ApplyMiddleware<OrderTrackingMiddleware1>()
                .ApplyMiddleware<OrderTrackingMiddleware2>()
                .ApplyMiddleware<OrderTrackingMiddleware3>();
        }
    }

    private class ModifyResultSchema : Schema
    {
        public ModifyResultSchema(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Query = new ModifyResultQuery();
        }
    }

    private class ModifyResultQuery : ObjectGraphType
    {
        public ModifyResultQuery()
        {
            Field<StringGraphType>("test")
                .Resolve(_ => "R")
                .ApplyMiddleware(next => async context =>
                {
                    var result = await next(context);
                    return result + "-F1";
                })
                .ApplyMiddleware(next => async context =>
                {
                    var result = await next(context);
                    return result + "-F2";
                });
        }
    }

    private class MultiFieldSchema : Schema
    {
        public MultiFieldSchema(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Query = new MultiFieldQuery(serviceProvider);
        }
    }

    private class MultiFieldQuery : ObjectGraphType
    {
        public MultiFieldQuery(IServiceProvider serviceProvider)
        {
            var executionOrder = serviceProvider.GetRequiredService<List<string>>();

            Field<StringGraphType>("field1")
                .Resolve(_ => "result1")
                .ApplyMiddleware(next => context =>
                {
                    executionOrder.Add("Field1-Middleware");
                    return next(context);
                });

            Field<StringGraphType>("field2")
                .Resolve(_ => "result2")
                .ApplyMiddleware(next => context =>
                {
                    executionOrder.Add("Field2-Middleware");
                    return next(context);
                });
        }
    }

    private class CombinedMiddlewareSchema : Schema
    {
        public CombinedMiddlewareSchema(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Query = new CombinedMiddlewareQuery(serviceProvider);
        }
    }

    private class CombinedMiddlewareQuery : ObjectGraphType
    {
        public CombinedMiddlewareQuery(IServiceProvider serviceProvider)
        {
            var executionOrder = serviceProvider.GetRequiredService<List<string>>();

            Field<StringGraphType>("test")
                .Resolve(_ => "result")
                .ApplyMiddleware(next => context =>
                {
                    executionOrder.Add("FieldLambda");
                    return next(context);
                })
                .ApplyMiddleware(new OrderTrackingMiddleware(executionOrder, "FieldClass"))
                .ApplyMiddleware<OrderTrackingMiddleware1>();
        }
    }

    // Middleware implementations
    private class OrderTrackingMiddleware : IFieldMiddleware
    {
        private readonly List<string> _executionOrder;
        private readonly string _name;

        public OrderTrackingMiddleware(List<string> executionOrder, string name)
        {
            _executionOrder = executionOrder;
            _name = name;
        }

        public ValueTask<object?> ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next)
        {
            _executionOrder.Add(_name);
            return next(context);
        }
    }

    private class OrderTrackingMiddleware1 : IFieldMiddleware
    {
        private readonly List<string> _executionOrder;

        public OrderTrackingMiddleware1(List<string> executionOrder)
        {
            _executionOrder = executionOrder;
        }

        public ValueTask<object?> ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next)
        {
            _executionOrder.Add("DI1");
            return next(context);
        }
    }

    private class OrderTrackingMiddleware2 : IFieldMiddleware
    {
        private readonly List<string> _executionOrder;

        public OrderTrackingMiddleware2(List<string> executionOrder)
        {
            _executionOrder = executionOrder;
        }

        public ValueTask<object?> ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next)
        {
            _executionOrder.Add("DI2");
            return next(context);
        }
    }

    private class OrderTrackingMiddleware3 : IFieldMiddleware
    {
        private readonly List<string> _executionOrder;

        public OrderTrackingMiddleware3(List<string> executionOrder)
        {
            _executionOrder = executionOrder;
        }

        public ValueTask<object?> ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next)
        {
            _executionOrder.Add("DI3");
            return next(context);
        }
    }
}
