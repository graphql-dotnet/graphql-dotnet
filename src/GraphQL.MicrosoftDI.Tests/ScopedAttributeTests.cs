using System;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace GraphQL.MicrosoftDI.Tests
{
    public class ScopedAttributeTests
    {
        [Fact]
        public async void ScopedMethodWorks()
        {
            Class1.DisposedCount = 0;
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<Class1>();
            serviceCollection.AddScoped<Class2>();
            var rootServiceProvider = serviceCollection.BuildServiceProvider(false);
            var graphType = new AutoRegisteringObjectGraphType<TestClass>();
            var context = new ResolveFieldContext
            {
                Source = new TestClass(),
                RequestServices = rootServiceProvider,
            };
            var unscopedFieldResolver = graphType.Fields.Find(nameof(TestClass.UnscopedField))!.Resolver!;
            var scopedFieldResolver = graphType.Fields.Find(nameof(TestClass.ScopedField))!.Resolver!;
            var scopedAsyncFieldResolver = graphType.Fields.Find(nameof(TestClass.ScopedAsyncField))!.Resolver!;
            unscopedFieldResolver.Resolve(context).ShouldBe("0 1");
            unscopedFieldResolver.Resolve(context).ShouldBe("1 2");
            unscopedFieldResolver.Resolve(context).ShouldBe("2 3");
            Class1.DisposedCount.ShouldBe(0);
            (await (Task<object>)scopedFieldResolver.Resolve(context)).ShouldBe("0 1");
            Class1.DisposedCount.ShouldBe(1);
            (await (Task<object>)scopedFieldResolver.Resolve(context)).ShouldBe("0 1");
            Class1.DisposedCount.ShouldBe(2);
            (await (Task<object>)scopedAsyncFieldResolver.Resolve(context)).ShouldBe("0 1");
            Class1.DisposedCount.ShouldBe(3);
            (await (Task<object>)scopedAsyncFieldResolver.Resolve(context)).ShouldBe("0 1");
            Class1.DisposedCount.ShouldBe(4);
            unscopedFieldResolver.Resolve(context).ShouldBe("3 4");
            rootServiceProvider.Dispose();
            Class1.DisposedCount.ShouldBe(5);
        }

        private class TestClass
        {
            public string UnscopedField([FromServices] Class1 arg1, [FromServices] Class2 arg2)
            {
                return $"{arg1.Value++} {arg2.Value}";
            }

            [Scoped]
            public string ScopedField([FromServices] Class1 arg1, [FromServices] Class2 arg2)
            {
                return $"{arg1.Value++} {arg2.Value}";
            }

            [Scoped]
            public async Task<string> ScopedAsyncField([FromServices] Class1 arg1, [FromServices] Class2 arg2)
            {
                await Task.Yield();
                return $"{arg1.Value++} {arg2.Value}";
            }
        }

        private class Class1 : IDisposable
        {
            public static int DisposedCount = 0;

            private bool _disposed = false;
            private int _value;

            public int Value
            {
                get => _disposed ? throw new ObjectDisposedException(null) : _value;
                set => _value = _disposed ? throw new ObjectDisposedException(null) : value;
            }

            public void Dispose()
            {
                if (!_disposed)
                    DisposedCount++;
                _disposed = true;
            }
        }

        private class Class2
        {
            private readonly Class1 _class1;

            public Class2(Class1 class1)
            {
                _class1 = class1;
            }

            public int Value => _class1.Value;
        }
    }
}
