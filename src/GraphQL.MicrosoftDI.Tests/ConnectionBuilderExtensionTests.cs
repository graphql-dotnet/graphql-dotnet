using GraphQL.Builders;
using GraphQL.Types;
using Moq;

namespace GraphQL.MicrosoftDI.Tests;

public class ConnectionBuilderExtensionTests : ScopedContextBase
{
    private readonly ResolveConnectionContext<object> _unscopedConnectionContext;

    public ConnectionBuilderExtensionTests()
    {
        _scopedServiceProviderMock.Setup(x => x.GetService(It.Is<Type>(x => x == typeof(string)))).Returns("hello");
        _scopedServiceProviderMock.Setup(x => x.GetService(It.Is<Type>(x => x == typeof(int)))).Returns(2);
        _scopedServiceProviderMock.Setup(x => x.GetService(It.Is<Type>(x => x == typeof(short)))).Returns((short)3);
        _scopedServiceProviderMock.Setup(x => x.GetService(It.Is<Type>(x => x == typeof(byte)))).Returns((byte)4);
        _scopedServiceProviderMock.Setup(x => x.GetService(It.Is<Type>(x => x == typeof(long)))).Returns((long)5);
        _unscopedConnectionContext = new ResolveConnectionContext<object>(
            new ResolveFieldContext<object>
            {
                RequestServices = _scopedServiceProvider
            },
            false,
            null);
    }

    private void VerifyUnscoped()
    {
        _scopedServiceProviderMock.Verify();
    }

    [Fact]
    public void WithScope0()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithScope()
            .Resolve(context => "hello");
        field.Resolver.ResolveAsync(_scopedContext).Result.ShouldBe("hello");
        VerifyScoped();
    }

    [Fact]
    public void WithScope0Alt()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder.ResolveScoped(context => "hello");
        field.Resolver.ResolveAsync(_scopedContext).Result.ShouldBe("hello");
        VerifyScoped();
    }

    [Fact]
    public void WithScope1()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithScope()
            .WithService<string>()
            .Resolve((context, value) => value);
        field.Resolver.ResolveAsync(_scopedContext).Result.ShouldBe("hello");
        VerifyScoped();
    }

    [Fact]
    public void WithScope2()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithScope()
            .WithService<string>()
            .WithService<int>()
            .Resolve((context, value, v2) => value + v2);
        field.Resolver.ResolveAsync(_scopedContext).Result.ShouldBe("hello2");
        VerifyScoped();
    }

    [Fact]
    public void WithScope3()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithScope()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .Resolve((context, value, v2, v3) => value + v2 + v3);
        field.Resolver.ResolveAsync(_scopedContext).Result.ShouldBe("hello23");
        VerifyScoped();
    }

    [Fact]
    public void WithScope4()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithScope()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .Resolve((context, value, v2, v3, v4) => value + v2 + v3 + v4);
        field.Resolver.ResolveAsync(_scopedContext).Result.ShouldBe("hello234");
        VerifyScoped();
    }

    [Fact]
    public void WithScope5()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithScope()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .WithService<long>()
            .Resolve((context, value, v2, v3, v4, v5) => value + v2 + v3 + v4 + v5);
        field.Resolver.ResolveAsync(_scopedContext).Result.ShouldBe("hello2345");
        VerifyScoped();
    }

    [Fact]
    public void WithScope2Alt()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithScope()
            .WithServices<string, int>()
            .Resolve((context, value, v2) => value + v2);
        field.Resolver.ResolveAsync(_scopedContext).Result.ShouldBe("hello2");
        VerifyScoped();
    }

    [Fact]
    public void WithScope3Alt()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithScope()
            .WithServices<string, int, short>()
            .Resolve((context, value, v2, v3) => value + v2 + v3);
        field.Resolver.ResolveAsync(_scopedContext).Result.ShouldBe("hello23");
        VerifyScoped();
    }

    [Fact]
    public void WithScope4Alt()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithScope()
            .WithServices<string, int, short, byte>()
            .Resolve((context, value, v2, v3, v4) => value + v2 + v3 + v4);
        field.Resolver.ResolveAsync(_scopedContext).Result.ShouldBe("hello234");
        VerifyScoped();
    }

    [Fact]
    public void WithScope5Alt()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithScope()
            .WithServices<string, int, short, byte, long>()
            .Resolve((context, value, v2, v3, v4, v5) => value + v2 + v3 + v4 + v5);
        field.Resolver.ResolveAsync(_scopedContext).Result.ShouldBe("hello2345");
        VerifyScoped();
    }

    [Fact]
    public void WithoutScope0()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .Resolve(context => "hello");
        field.Resolver.ResolveAsync(_unscopedConnectionContext).Result.ShouldBe("hello");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope1()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .Resolve((context, value) => value);
        field.Resolver.ResolveAsync(_unscopedConnectionContext).Result.ShouldBe("hello");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope2()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .Resolve((context, value, v2) => value + v2);
        field.Resolver.ResolveAsync(_unscopedConnectionContext).Result.ShouldBe("hello2");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope3()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .Resolve((context, value, v2, v3) => value + v2 + v3);
        field.Resolver.ResolveAsync(_unscopedConnectionContext).Result.ShouldBe("hello23");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope4()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .Resolve((context, value, v2, v3, v4) => value + v2 + v3 + v4);
        field.Resolver.ResolveAsync(_unscopedConnectionContext).Result.ShouldBe("hello234");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope5()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .WithService<long>()
            .Resolve((context, value, v2, v3, v4, v5) => value + v2 + v3 + v4 + v5);
        field.Resolver.ResolveAsync(_unscopedConnectionContext).Result.ShouldBe("hello2345");
        VerifyUnscoped();
    }

    [Fact]
    public void WithScope0Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithScope()
            .ResolveAsync(context => Task.FromResult<object>("hello"));
        field.Resolver.ResolveAsync(_scopedContext).ShouldBeTask("hello");
        VerifyScoped();
    }

    [Fact]
    public void WithScope0AsyncAlt()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder.ResolveScopedAsync(context => Task.FromResult<object>("hello"));
        field.Resolver.ResolveAsync(_scopedContext).ShouldBeTask("hello");
        VerifyScoped();
    }

    [Fact]
    public void WithScope1Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithScope()
            .ResolveAsync((context, value) => Task.FromResult<object>(value));
        field.Resolver.ResolveAsync(_scopedContext).ShouldBeTask("hello");
        VerifyScoped();
    }

    [Fact]
    public void WithScope2Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithScope()
            .ResolveAsync((context, value, v2) => Task.FromResult<object>(value + v2));
        field.Resolver.ResolveAsync(_scopedContext).ShouldBeTask("hello2");
        VerifyScoped();
    }

    [Fact]
    public void WithScope3Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithScope()
            .ResolveAsync((context, value, v2, v3) => Task.FromResult<object>(value + v2 + v3));
        field.Resolver.ResolveAsync(_scopedContext).ShouldBeTask("hello23");
        VerifyScoped();
    }

    [Fact]
    public void WithScope4Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .WithScope()
            .ResolveAsync((context, value, v2, v3, v4) => Task.FromResult<object>(value + v2 + v3 + v4));
        field.Resolver.ResolveAsync(_scopedContext).ShouldBeTask("hello234");
        VerifyScoped();
    }

    [Fact]
    public void WithScope5Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .WithService<long>()
            .WithScope()
            .ResolveAsync((context, value, v2, v3, v4, v5) => Task.FromResult<object>(value + v2 + v3 + v4 + v5));
        field.Resolver.ResolveAsync(_scopedContext).ShouldBeTask("hello2345");
        VerifyScoped();
    }

    [Fact]
    public void WithoutScope0Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .ResolveAsync(context => Task.FromResult<object>("hello"));
        field.Resolver.ResolveAsync(_unscopedConnectionContext).ShouldBeTask("hello");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope1Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .ResolveAsync((context, value) => Task.FromResult<object>(value));
        field.Resolver.ResolveAsync(_unscopedConnectionContext).ShouldBeTask("hello");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope2Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .ResolveAsync((context, value, v2) => Task.FromResult<object>(value + v2));
        field.Resolver.ResolveAsync(_unscopedConnectionContext).ShouldBeTask("hello2");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope3Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .ResolveAsync((context, value, v2, v3) => Task.FromResult<object>(value + v2 + v3));
        field.Resolver.ResolveAsync(_unscopedConnectionContext).ShouldBeTask("hello23");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope4Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .ResolveAsync((context, value, v2, v3, v4) => Task.FromResult<object>(value + v2 + v3 + v4));
        field.Resolver.ResolveAsync(_unscopedConnectionContext).ShouldBeTask("hello234");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope5Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>();
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .WithService<long>()
            .ResolveAsync((context, value, v2, v3, v4, v5) => Task.FromResult<object>(value + v2 + v3 + v4 + v5));
        field.Resolver.ResolveAsync(_unscopedConnectionContext).ShouldBeTask("hello2345");
        VerifyUnscoped();
    }
}
