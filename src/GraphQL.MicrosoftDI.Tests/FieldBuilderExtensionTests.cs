using GraphQL.Types;
using Moq;

namespace GraphQL.MicrosoftDI.Tests;

public class FieldBuilderExtensionTests : ScopedContextBase
{
    private readonly ResolveFieldContext _unscopedContext;

    public FieldBuilderExtensionTests()
    {
        _scopedServiceProviderMock.Setup(x => x.GetService(It.Is<Type>(x => x == typeof(string)))).Returns("hello");
        _scopedServiceProviderMock.Setup(x => x.GetService(It.Is<Type>(x => x == typeof(int)))).Returns(2);
        _scopedServiceProviderMock.Setup(x => x.GetService(It.Is<Type>(x => x == typeof(short)))).Returns((short)3);
        _scopedServiceProviderMock.Setup(x => x.GetService(It.Is<Type>(x => x == typeof(byte)))).Returns((byte)4);
        _scopedServiceProviderMock.Setup(x => x.GetService(It.Is<Type>(x => x == typeof(long)))).Returns((long)5);
        _unscopedContext = new ResolveFieldContext
        {
            RequestServices = _scopedServiceProvider
        };
    }

    private void VerifyUnscoped()
    {
        _scopedServiceProviderMock.Verify();
    }

    [Fact]
    public void WithScope0()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithScope()
            .Resolve(context => "hello")
            .FieldType;
        field.Resolver.ResolveAsync(_scopedContext).Result.ShouldBe("hello");
        VerifyScoped();
    }

    [Fact]
    public void WithScope1()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithScope()
            .WithService<string>()
            .Resolve((context, value) => value)
            .FieldType;
        field.Resolver.ResolveAsync(_scopedContext).Result.ShouldBe("hello");
        VerifyScoped();
    }

    [Fact]
    public void WithScope2()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithScope()
            .WithService<string>()
            .WithService<int>()
            .Resolve((context, value, v2) => value + v2)
            .FieldType;
        field.Resolver.ResolveAsync(_scopedContext).Result.ShouldBe("hello2");
        VerifyScoped();
    }

    [Fact]
    public void WithScope3()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithScope()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .Resolve((context, value, v2, v3) => value + v2 + v3)
            .FieldType;
        field.Resolver.ResolveAsync(_scopedContext).Result.ShouldBe("hello23");
        VerifyScoped();
    }

    [Fact]
    public void WithScope4()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithScope()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .Resolve((context, value, v2, v3, v4) => value + v2 + v3 + v4)
            .FieldType;
        field.Resolver.ResolveAsync(_scopedContext).Result.ShouldBe("hello234");
        VerifyScoped();
    }

    [Fact]
    public void WithScope5()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithScope()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .WithService<long>()
            .Resolve((context, value, v2, v3, v4, v5) => value + v2 + v3 + v4 + v5)
            .FieldType;
        field.Resolver.ResolveAsync(_scopedContext).Result.ShouldBe("hello2345");
        VerifyScoped();
    }

    [Fact]
    public void WithScope2Alt()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithScope()
            .WithServices<string, int>()
            .Resolve((context, value, v2) => value + v2)
            .FieldType;
        field.Resolver.ResolveAsync(_scopedContext).Result.ShouldBe("hello2");
        VerifyScoped();
    }

    [Fact]
    public void WithScope3Alt()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithScope()
            .WithServices<string, int, short>()
            .Resolve((context, value, v2, v3) => value + v2 + v3)
            .FieldType;
        field.Resolver.ResolveAsync(_scopedContext).Result.ShouldBe("hello23");
        VerifyScoped();
    }

    [Fact]
    public void WithScope4Alt()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithScope()
            .WithServices<string, int, short, byte>()
            .Resolve((context, value, v2, v3, v4) => value + v2 + v3 + v4)
            .FieldType;
        field.Resolver.ResolveAsync(_scopedContext).Result.ShouldBe("hello234");
        VerifyScoped();
    }

    [Fact]
    public void WithScope5Alt()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithScope()
            .WithServices<string, int, short, byte, long>()
            .Resolve((context, value, v2, v3, v4, v5) => value + v2 + v3 + v4 + v5)
            .FieldType;
        field.Resolver.ResolveAsync(_scopedContext).Result.ShouldBe("hello2345");
        VerifyScoped();
    }

    [Fact]
    public void WithoutScope0()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .Resolve(context => "hello")
            .FieldType;
        field.Resolver.ResolveAsync(_unscopedContext).Result.ShouldBe("hello");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope1()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithService<string>()
            .Resolve((context, value) => value)
            .FieldType;
        field.Resolver.ResolveAsync(_unscopedContext).Result.ShouldBe("hello");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope2()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .Resolve((context, value, v2) => value + v2)
            .FieldType;
        field.Resolver.ResolveAsync(_unscopedContext).Result.ShouldBe("hello2");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope3()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .Resolve((context, value, v2, v3) => value + v2 + v3)
            .FieldType;
        field.Resolver.ResolveAsync(_unscopedContext).Result.ShouldBe("hello23");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope4()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .Resolve((context, value, v2, v3, v4) => value + v2 + v3 + v4)
            .FieldType;
        field.Resolver.ResolveAsync(_unscopedContext).Result.ShouldBe("hello234");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope5()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .WithService<long>()
            .Resolve((context, value, v2, v3, v4, v5) => value + v2 + v3 + v4 + v5)
            .FieldType;
        field.Resolver.ResolveAsync(_unscopedContext).Result.ShouldBe("hello2345");
        VerifyUnscoped();
    }

    [Fact]
    public void WithScope0Async()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithScope()
            .ResolveAsync(context => Task.FromResult<object>("hello"))
            .FieldType;
        field.Resolver.ResolveAsync(_scopedContext).ShouldBeTask("hello");
        VerifyScoped();
    }

    [Fact]
    public void WithScope1Async()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithService<string>()
            .WithScope()
            .ResolveAsync((context, value) => Task.FromResult<object>(value))
            .FieldType;
        field.Resolver.ResolveAsync(_scopedContext).ShouldBeTask("hello");
        VerifyScoped();
    }

    [Fact]
    public void WithScope2Async()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithScope()
            .ResolveAsync((context, value, v2) => Task.FromResult<object>(value + v2))
            .FieldType;
        field.Resolver.ResolveAsync(_scopedContext).ShouldBeTask("hello2");
        VerifyScoped();
    }

    [Fact]
    public void WithScope3Async()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithScope()
            .ResolveAsync((context, value, v2, v3) => Task.FromResult<object>(value + v2 + v3))
            .FieldType;
        field.Resolver.ResolveAsync(_scopedContext).ShouldBeTask("hello23");
        VerifyScoped();
    }

    [Fact]
    public void WithScope4Async()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .WithScope()
            .ResolveAsync((context, value, v2, v3, v4) => Task.FromResult<object>(value + v2 + v3 + v4))
            .FieldType;
        field.Resolver.ResolveAsync(_scopedContext).ShouldBeTask("hello234");
        VerifyScoped();
    }

    [Fact]
    public void WithScope5Async()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .WithService<long>()
            .WithScope()
            .ResolveAsync((context, value, v2, v3, v4, v5) => Task.FromResult<object>(value + v2 + v3 + v4 + v5))
            .FieldType;
        field.Resolver.ResolveAsync(_scopedContext).ShouldBeTask("hello2345");
        VerifyScoped();
    }

    [Fact]
    public void WithoutScope0Async()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .ResolveAsync(context => Task.FromResult<object>("hello"))
            .FieldType;
        field.Resolver.ResolveAsync(_unscopedContext).ShouldBeTask("hello");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope1Async()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithService<string>()
            .ResolveAsync((context, value) => Task.FromResult<object>(value))
            .FieldType;
        field.Resolver.ResolveAsync(_unscopedContext).ShouldBeTask("hello");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope2Async()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .ResolveAsync((context, value, v2) => Task.FromResult<object>(value + v2))
            .FieldType;
        field.Resolver.ResolveAsync(_unscopedContext).ShouldBeTask("hello2");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope3Async()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .ResolveAsync((context, value, v2, v3) => Task.FromResult<object>(value + v2 + v3))
            .FieldType;
        field.Resolver.ResolveAsync(_unscopedContext).ShouldBeTask("hello23");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope4Async()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .ResolveAsync((context, value, v2, v3, v4) => Task.FromResult<object>(value + v2 + v3 + v4))
            .FieldType;
        field.Resolver.ResolveAsync(_unscopedContext).ShouldBeTask("hello234");
        VerifyUnscoped();
    }

    [Fact]
    public void WithoutScope5Async()
    {
        var graph = new ObjectGraphType();
        var field = graph.Field<StringGraphType>("_")
            .Resolve()
            .WithService<string>()
            .WithService<int>()
            .WithService<short>()
            .WithService<byte>()
            .WithService<long>()
            .ResolveAsync((context, value, v2, v3, v4, v5) => Task.FromResult<object>(value + v2 + v3 + v4 + v5))
            .FieldType;
        field.Resolver.ResolveAsync(_unscopedContext).ShouldBeTask("hello2345");
        VerifyUnscoped();
    }
}
