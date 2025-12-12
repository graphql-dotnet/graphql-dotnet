using System.Reactive.Linq;
using System.Runtime.CompilerServices;
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
        accessor.Context.ShouldBeNull();
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
        accessor.Context.ShouldBeNull();
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

    [Theory]
    [InlineData("streamField1")]
    [InlineData("streamField2")]
    public async Task ContextAccessor_StreamResolver_ReturnsCorrectContext(string fieldName)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddSchema<TestStreamSchema>()
            .AddResolveFieldContextAccessor()
            .AddSystemTextJson());

        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        schema.Initialize();
        var accessor = provider.GetRequiredService<IResolveFieldContextAccessor>();
        var executer = provider.GetRequiredService<IDocumentExecuter>();
        var serializer = provider.GetRequiredService<IGraphQLTextSerializer>();

        // Act
        var result = await executer.ExecuteAsync(_ =>
        {
            _.Schema = schema;
            _.Query = $"subscription {{ {fieldName} }}";
            _.RequestServices = provider;
        });

        // Assert
        result.Streams.ShouldNotBeNull();
        result.Streams.Count.ShouldBe(1);

        var stream = result.Streams.Single().Value;
        var streamList = await stream.ToList();
        // Verify the stream emitted the correct value
        streamList.Count.ShouldBe(2);
        var row = serializer.Serialize(streamList[0]);
        row.ShouldBeCrossPlatJson($$$"""{"data":{"{{{fieldName}}}":"found"}}""");
        row = serializer.Serialize(streamList[1]);
        row.ShouldBeCrossPlatJson($$$"""{"data":{"{{{fieldName}}}":"found"}}""");

        // Context should be null after execution completes
        accessor.Context.ShouldBeNull();
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

    private class TestStreamSchema : Schema
    {
        public TestStreamSchema(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Query = new TestStreamQuery();
            Subscription = new TestStreamSubscription();
        }
    }

    private class TestStreamQuery : ObjectGraphType
    {
        public TestStreamQuery()
        {
            Field<StringGraphType>("dummy").Resolve(_ => "dummy");
        }
    }

    private class TestStreamSubscription : AutoRegisteringObjectGraphType<TestStreamSubscriptionModel>
    {
        public TestStreamSubscription()
        {
            Field<string>("streamField2")
                .ResolveStream(ctx => new MyObservable(ctx.RequestServices!.GetRequiredService<IResolveFieldContextAccessor>()));
        }

        private class MyObservable : IObservable<string>
        {
            private readonly IResolveFieldContextAccessor _accessor;
            public MyObservable(IResolveFieldContextAccessor accessor)
            {
                _accessor = accessor;
            }
            public IDisposable Subscribe(IObserver<string> observer)
            {
                SendData(observer);
                return new DummyDisposable();
            }

            private class DummyDisposable : IDisposable
            {
                public void Dispose()
                {
                }
            }

            private async void SendData(IObserver<string> observer)
            {
                observer.OnNext(_accessor.Context != null ? "found" : "not found");
                await Task.Yield();
                observer.OnNext(_accessor.Context != null ? "found" : "not found");
                observer.OnCompleted();
            }
        }
    }

    private class TestStreamSubscriptionModel
    {
        public static async IAsyncEnumerable<string> StreamField1([FromServices] IResolveFieldContextAccessor accessor, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return accessor.Context != null ? "found" : "not found";
            await Task.Yield(); // Simulate async work
            cancellationToken.ThrowIfCancellationRequested();
            yield return accessor.Context != null ? "found" : "not found";
        }
    }
}
