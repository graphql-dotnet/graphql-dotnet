using System;
using GraphQL.MicrosoftDI;
using GraphQL.Types;
using Moq;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.MicrosoftDI
{
    public class FieldBuilderExtensionTests : ScopedContextBase
    {
        private readonly ResolveFieldContext _unscopedContext;

        public FieldBuilderExtensionTests() : base()
        {
            _scopedServiceProviderMock.Setup(x => x.GetService(It.Is<Type>(x => x == typeof(string)))).Returns("hello").Verifiable();
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
        public void WithScope1()
        {
            var graph = new ObjectGraphType();
            var field = graph.Field<StringGraphType>()
                .Resolve()
                .WithScope()
                .WithType<string>()
                .Resolve((context, value) => value)
                .FieldType;
            field.Resolver.Resolve(_scopedContext).ShouldBe("hello");
            VerifyScoped();
        }

        [Fact]
        public void WithScope2()
        {
            var graph = new ObjectGraphType();
            var field = graph.Field<StringGraphType>()
                .Resolve()
                .WithScope()
                .WithType<string>()
                .WithType<int>()
                .Resolve((context, value, v2) => value + v2)
                .FieldType;
            field.Resolver.Resolve(_scopedContext).ShouldBe("hello2");
            VerifyScoped();
        }

        [Fact]
        public void WithScope3()
        {
            var graph = new ObjectGraphType();
            var field = graph.Field<StringGraphType>()
                .Resolve()
                .WithScope()
                .WithType<string>()
                .WithType<int>()
                .WithType<short>()
                .Resolve((context, value, v2, v3) => value + v2 + v3)
                .FieldType;
            field.Resolver.Resolve(_scopedContext).ShouldBe("hello23");
            VerifyScoped();
        }

        [Fact]
        public void WithScope4()
        {
            var graph = new ObjectGraphType();
            var field = graph.Field<StringGraphType>()
                .Resolve()
                .WithScope()
                .WithType<string>()
                .WithType<int>()
                .WithType<short>()
                .WithType<byte>()
                .Resolve((context, value, v2, v3, v4) => value + v2 + v3 + v4)
                .FieldType;
            field.Resolver.Resolve(_scopedContext).ShouldBe("hello234");
            VerifyScoped();
        }

        [Fact]
        public void WithScope5()
        {
            var graph = new ObjectGraphType();
            var field = graph.Field<StringGraphType>()
                .Resolve()
                .WithScope()
                .WithType<string>()
                .WithType<int>()
                .WithType<short>()
                .WithType<byte>()
                .WithType<long>()
                .Resolve((context, value, v2, v3, v4, v5) => value + v2 + v3 + v4 + v5)
                .FieldType;
            field.Resolver.Resolve(_scopedContext).ShouldBe("hello2345");
            VerifyScoped();
        }

        [Fact]
        public void WithoutScope1()
        {
            var graph = new ObjectGraphType();
            var field = graph.Field<StringGraphType>()
                .Resolve()
                .WithType<string>()
                .Resolve((context, value) => value)
                .FieldType;
            field.Resolver.Resolve(_unscopedContext).ShouldBe("hello");
            VerifyUnscoped();
        }

        [Fact]
        public void WithoutScope2()
        {
            var graph = new ObjectGraphType();
            var field = graph.Field<StringGraphType>()
                .Resolve()
                .WithType<string>()
                .WithType<int>()
                .Resolve((context, value, v2) => value + v2)
                .FieldType;
            field.Resolver.Resolve(_unscopedContext).ShouldBe("hello2");
            VerifyUnscoped();
        }

        [Fact]
        public void WithoutScope3()
        {
            var graph = new ObjectGraphType();
            var field = graph.Field<StringGraphType>()
                .Resolve()
                .WithType<string>()
                .WithType<int>()
                .WithType<short>()
                .Resolve((context, value, v2, v3) => value + v2 + v3)
                .FieldType;
            field.Resolver.Resolve(_unscopedContext).ShouldBe("hello23");
            VerifyUnscoped();
        }

        [Fact]
        public void WithoutScope4()
        {
            var graph = new ObjectGraphType();
            var field = graph.Field<StringGraphType>()
                .Resolve()
                .WithType<string>()
                .WithType<int>()
                .WithType<short>()
                .WithType<byte>()
                .Resolve((context, value, v2, v3, v4) => value + v2 + v3 + v4)
                .FieldType;
            field.Resolver.Resolve(_unscopedContext).ShouldBe("hello234");
            VerifyUnscoped();
        }

        [Fact]
        public void WithoutScope5()
        {
            var graph = new ObjectGraphType();
            var field = graph.Field<StringGraphType>()
                .Resolve()
                .WithType<string>()
                .WithType<int>()
                .WithType<short>()
                .WithType<byte>()
                .WithType<long>()
                .Resolve((context, value, v2, v3, v4, v5) => value + v2 + v3 + v4 + v5)
                .FieldType;
            field.Resolver.Resolve(_unscopedContext).ShouldBe("hello2345");
            VerifyUnscoped();
        }
    }
}
