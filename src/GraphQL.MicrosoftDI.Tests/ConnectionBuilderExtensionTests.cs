using GraphQL.Builders;
using GraphQL.Types;
using Moq;

namespace GraphQL.MicrosoftDI.Tests;

#pragma warning disable RCS1047 // Non-asynchronous method name should not end with 'Async'.

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
    public async Task WithScope0()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithScope()
            .Resolve(_ => "hello");
        (await field.Resolver.ResolveAsync(_scopedContext)).ShouldBe("hello");
        VerifyScoped();
    }

    [Fact]
    public async Task WithScope0Alt()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder.ResolveScoped(_ => "hello");
        (await field.Resolver.ResolveAsync(_scopedContext)).ShouldBe("hello");
        VerifyScoped();
    }

    [Fact]
    public async Task WithScope1()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithScope()
            .WithService<string>()
            .Resolve((_, value) => value);
        (await field.Resolver.ResolveAsync(_scopedContext)).ShouldBe("hello");
        VerifyScoped();
    }

    [Fact]
    public async Task WithScope2()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithScope()
            .WithService<string>()
            .WithService<int>()
            .Resolve((_, value, v2) => value + v2);
        (await field.Resolver.ResolveAsync(_scopedContext)).ShouldBe("hello2");
        VerifyScoped();
    }

    [Fact]
    public async Task WithScope3()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithScope()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .Resolve((_, value, v2, v3) => value + v2 + v3);
        (await field.Resolver.ResolveAsync(_scopedContext)).ShouldBe("hello23");
        VerifyScoped();
    }

    [Fact]
    public async Task WithScope4()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithScope()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .Resolve((_, value, v2, v3, v4) => value + v2 + v3 + v4);
        (await field.Resolver.ResolveAsync(_scopedContext)).ShouldBe("hello234");
        VerifyScoped();
    }

    [Fact]
    public async Task WithScope5()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithScope()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .WithService<long>()
            .Resolve((_, value, v2, v3, v4, v5) => value + v2 + v3 + v4 + v5);
        (await field.Resolver.ResolveAsync(_scopedContext)).ShouldBe("hello2345");
        VerifyScoped();
    }

    [Fact]
    public async Task WithScope2Alt()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithScope()
            .WithServices<string, int>()
            .Resolve((_, value, v2) => value + v2);
        (await field.Resolver.ResolveAsync(_scopedContext)).ShouldBe("hello2");
        VerifyScoped();
    }

    [Fact]
    public async Task WithScope3Alt()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithScope()
            .WithServices<string, int, short>()
            .Resolve((_, value, v2, v3) => value + v2 + v3);
        (await field.Resolver.ResolveAsync(_scopedContext)).ShouldBe("hello23");
        VerifyScoped();
    }

    [Fact]
    public async Task WithScope4Alt()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithScope()
            .WithServices<string, int, short, byte>()
            .Resolve((_, value, v2, v3, v4) => value + v2 + v3 + v4);
        (await field.Resolver.ResolveAsync(_scopedContext)).ShouldBe("hello234");
        VerifyScoped();
    }

    [Fact]
    public async Task WithScope5Alt()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithScope()
            .WithServices<string, int, short, byte, long>()
            .Resolve((_, value, v2, v3, v4, v5) => value + v2 + v3 + v4 + v5);
        (await field.Resolver.ResolveAsync(_scopedContext)).ShouldBe("hello2345");
        VerifyScoped();
    }

    [Fact]
    public async Task WithoutScope0()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .Resolve(_ => "hello");
        (await field.Resolver.ResolveAsync(_unscopedConnectionContext)).ShouldBe("hello");
        VerifyUnscoped();
    }

    [Fact]
    public async Task WithoutScope1()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .Resolve((_, value) => value);
        (await field.Resolver.ResolveAsync(_unscopedConnectionContext)).ShouldBe("hello");
        VerifyUnscoped();
    }

    [Fact]
    public async Task WithoutScope2()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .Resolve((_, value, v2) => value + v2);
        (await field.Resolver.ResolveAsync(_unscopedConnectionContext)).ShouldBe("hello2");
        VerifyUnscoped();
    }

    [Fact]
    public async Task WithoutScope3()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .Resolve((_, value, v2, v3) => value + v2 + v3);
        (await field.Resolver.ResolveAsync(_unscopedConnectionContext)).ShouldBe("hello23");
        VerifyUnscoped();
    }

    [Fact]
    public async Task WithoutScope4()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .Resolve((_, value, v2, v3, v4) => value + v2 + v3 + v4);
        (await field.Resolver.ResolveAsync(_unscopedConnectionContext)).ShouldBe("hello234");
        VerifyUnscoped();
    }

    [Fact]
    public async Task WithoutScope5()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .WithService<long>()
            .Resolve((_, value, v2, v3, v4, v5) => value + v2 + v3 + v4 + v5);
        (await field.Resolver.ResolveAsync(_unscopedConnectionContext)).ShouldBe("hello2345");
        VerifyUnscoped();
    }

    [Fact]
    public void WithScope0Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithScope()
            .ResolveAsync(_ => Task.FromResult<object>("hello"));
        field.Resolver.ResolveAsync(_scopedContext).ShouldBeTask("hello");
        VerifyScoped();
    }

    [Fact]
    public void WithScope0AsyncAlt()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder.ResolveScopedAsync(_ => Task.FromResult<object>("hello"));
        field.Resolver.ResolveAsync(_scopedContext).ShouldBeTask("hello");
        VerifyScoped();
    }

    [Fact]
    public void WithScope1Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithScope()
            .ResolveAsync((_, value) => Task.FromResult<object>(value));
        field.Resolver.ResolveAsync(_scopedContext).ShouldBeTask("hello");
        VerifyScoped();
    }

    [Fact]
    public void WithScope2Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithScope()
            .ResolveAsync((_, value, v2) => Task.FromResult<object>(value + v2));
        field.Resolver.ResolveAsync(_scopedContext).ShouldBeTask("hello2");
        VerifyScoped();
    }

    [Fact]
    public void WithScope3Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithScope()
            .ResolveAsync((_, value, v2, v3) => Task.FromResult<object>(value + v2 + v3));
        field.Resolver.ResolveAsync(_scopedContext).ShouldBeTask("hello23");
        VerifyScoped();
    }

    [Fact]
    public void WithScope4Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .WithScope()
            .ResolveAsync((_, value, v2, v3, v4) => Task.FromResult<object>(value + v2 + v3 + v4));
        field.Resolver.ResolveAsync(_scopedContext).ShouldBeTask("hello234");
        VerifyScoped();
    }

    [Fact]
    public void WithScope5Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .WithService<long>()
            .WithScope()
            .ResolveAsync((_, value, v2, v3, v4, v5) => Task.FromResult<object>(value + v2 + v3 + v4 + v5));
        field.Resolver.ResolveAsync(_scopedContext).ShouldBeTask("hello2345");
        VerifyScoped();
    }

    [Fact]
    public void WithoutScope0Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .ResolveAsync(_ => Task.FromResult<object>("hello"));
        field.Resolver.ResolveAsync(_unscopedConnectionContext).ShouldBeTask("hello");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope1Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .ResolveAsync((_, value) => Task.FromResult<object>(value));
        field.Resolver.ResolveAsync(_unscopedConnectionContext).ShouldBeTask("hello");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope2Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .ResolveAsync((_, value, v2) => Task.FromResult<object>(value + v2));
        field.Resolver.ResolveAsync(_unscopedConnectionContext).ShouldBeTask("hello2");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope3Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .ResolveAsync((_, value, v2, v3) => Task.FromResult<object>(value + v2 + v3));
        field.Resolver.ResolveAsync(_unscopedConnectionContext).ShouldBeTask("hello23");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope4Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .ResolveAsync((_, value, v2, v3, v4) => Task.FromResult<object>(value + v2 + v3 + v4));
        field.Resolver.ResolveAsync(_unscopedConnectionContext).ShouldBeTask("hello234");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope5Async()
    {
        var graph = new ObjectGraphType();
        var builder = graph.Connection<StringGraphType>("connection");
        var field = builder.FieldType;
        builder
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .WithService<long>()
            .ResolveAsync((_, value, v2, v3, v4, v5) => Task.FromResult<object>(value + v2 + v3 + v4 + v5));
        field.Resolver.ResolveAsync(_unscopedConnectionContext).ShouldBeTask("hello2345");
        VerifyUnscoped();
    }
}
