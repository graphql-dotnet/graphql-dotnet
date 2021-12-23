using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.Resolvers;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Execution
{
    public class FuncFieldResolverTests
    {
        private readonly ResolveFieldContext _context;
        private readonly OkDataLoader _okDataLoader = new OkDataLoader();

        private class OkDataLoader : IDataLoaderResult<string>
        {
            Task<string> IDataLoaderResult<string>.GetResultAsync(CancellationToken cancellationToken) => Task.FromResult("ok");
            Task<object> IDataLoaderResult.GetResultAsync(CancellationToken cancellationToken) => Task.FromResult<object>("ok");
        }

        public FuncFieldResolverTests()
        {
            _context = new ResolveFieldContext
            {
                Arguments = new Dictionary<string, ArgumentValue>(),
                Errors = new ExecutionErrors(),
                Extensions = new Dictionary<string, object>(),
            };
        }

        [Fact]
        public void Pass_Through_Object_Source()
        {
            IResolveFieldContext<object> rfc1 = null;
            var ffr1 = new FuncFieldResolver<object, string>(context =>
            {
                rfc1 = context;
                return "ok";
            });
            ffr1.Resolve(_context);
            rfc1.ShouldNotBeNull();
            rfc1.ShouldBeSameAs(_context);
        }

        [Fact]
        public void Shares_Complete_Typed()
        {
            IResolveFieldContext<int?> rfc1 = null;
            IResolveFieldContext<int?> rfc2 = null;
            var ffr1 = new FuncFieldResolver<int?, string>(context =>
            {
                rfc1 = context;
                return "ok";
            });
            var ffr2 = new FuncFieldResolver<int?, string>(context =>
            {
                rfc2 = context;
                return "ok";
            });
            ffr1.Resolve(_context);
            ffr2.Resolve(_context);
            rfc1.ShouldNotBeNull();
            rfc1.ShouldNotBeSameAs(_context);
            rfc2.ShouldNotBeNull();
            rfc2.ShouldNotBeSameAs(_context);
            rfc1.ShouldBe(rfc2);
        }

        [Fact]
        public void Shares_Complete_Untyped()
        {
            IResolveFieldContext<int?> rfc1 = null;
            IResolveFieldContext<int?> rfc2 = null;
            var ffr1 = new FuncFieldResolver<int?, object>(context =>
            {
                rfc1 = context;
                return "ok";
            });
            var ffr2 = new FuncFieldResolver<int?, object>(context =>
            {
                rfc2 = context;
                return "ok";
            });
            ffr1.Resolve(_context);
            ffr2.Resolve(_context);
            rfc1.ShouldNotBeNull();
            rfc1.ShouldNotBeSameAs(_context);
            rfc2.ShouldNotBeNull();
            rfc2.ShouldNotBeSameAs(_context);
            rfc1.ShouldBe(rfc2);
        }

        [Fact]
        public void Does_Not_Share_Failed_Typed()
        {
            IResolveFieldContext<int?> rfc1 = null;
            IResolveFieldContext<int?> rfc2 = null;
            var ffr1 = new FuncFieldResolver<int?, string>(context =>
            {
                rfc1 = context;
                throw new Exception();
            });
            var ffr2 = new FuncFieldResolver<int?, string>(context =>
            {
                rfc2 = context;
                return "ok";
            });
            try
            {
                ffr1.Resolve(_context);
            }
            catch { }
            ffr2.Resolve(_context);
            rfc1.ShouldNotBeNull();
            rfc1.ShouldNotBeSameAs(_context);
            rfc2.ShouldNotBeNull();
            rfc2.ShouldNotBeSameAs(_context);
            rfc1.ShouldNotBe(rfc2);
        }

        [Fact]
        public void Does_Not_Share_Failed_Untyped()
        {
            IResolveFieldContext<int?> rfc1 = null;
            IResolveFieldContext<int?> rfc2 = null;
            var ffr1 = new FuncFieldResolver<int?, object>(context =>
            {
                rfc1 = context;
                throw new Exception();
            });
            var ffr2 = new FuncFieldResolver<int?, object>(context =>
            {
                rfc2 = context;
                return "ok";
            });
            try
            {
                ffr1.Resolve(_context);
            }
            catch { }
            ffr2.Resolve(_context);
            rfc1.ShouldNotBeNull();
            rfc1.ShouldNotBeSameAs(_context);
            rfc2.ShouldNotBeNull();
            rfc2.ShouldNotBeSameAs(_context);
            rfc1.ShouldNotBe(rfc2);
        }

        [Fact]
        public void Shares_Complete_Tasks_Typed()
        {
            IResolveFieldContext<int?> rfc1 = null;
            IResolveFieldContext<int?> rfc2 = null;
            var ffr1 = new FuncFieldResolver<int?, Task<string>>(context =>
            {
                rfc1 = context;
                return Task.FromResult("ok");
            });
            var ffr2 = new FuncFieldResolver<int?, Task<string>>(context =>
            {
                rfc2 = context;
                return Task.FromResult("ok");
            });
            ffr1.Resolve(_context);
            ffr2.Resolve(_context);
            rfc1.ShouldNotBeNull();
            rfc1.ShouldNotBeSameAs(_context);
            rfc2.ShouldNotBeNull();
            rfc2.ShouldNotBeSameAs(_context);
            rfc1.ShouldBe(rfc2);
        }

        [Fact]
        public void Shares_Complete_Tasks_Untyped()
        {
            IResolveFieldContext<int?> rfc1 = null;
            IResolveFieldContext<int?> rfc2 = null;
            var ffr1 = new FuncFieldResolver<int?, object>(context =>
            {
                rfc1 = context;
                return Task.FromResult("ok");
            });
            var ffr2 = new FuncFieldResolver<int?, object>(context =>
            {
                rfc2 = context;
                return Task.FromResult("ok");
            });
            ffr1.Resolve(_context);
            ffr2.Resolve(_context);
            rfc1.ShouldNotBeNull();
            rfc1.ShouldNotBeSameAs(_context);
            rfc2.ShouldNotBeNull();
            rfc2.ShouldNotBeSameAs(_context);
            rfc1.ShouldBe(rfc2);
        }

        [Fact]
        public void Does_Not_Share_Incomplete_Tasks_Typed()
        {
            IResolveFieldContext<int?> rfc1 = null;
            IResolveFieldContext<int?> rfc2 = null;
            var ffr1 = new FuncFieldResolver<int?, Task<string>>(async context =>
            {
                rfc1 = context;
                await Task.Delay(100);
                return "ok";
            });
            var ffr2 = new FuncFieldResolver<int?, Task<string>>(async context =>
            {
                rfc2 = context;
                await Task.Delay(100);
                return "ok";
            });
            ffr1.Resolve(_context);
            ffr2.Resolve(_context);
            rfc1.ShouldNotBeNull();
            rfc1.ShouldNotBeSameAs(_context);
            rfc2.ShouldNotBeNull();
            rfc2.ShouldNotBeSameAs(_context);
            rfc1.ShouldNotBe(rfc2);
        }

        [Fact]
        public void Does_Not_Share_Incomplete_Tasks_Untyped()
        {
            IResolveFieldContext<int?> rfc1 = null;
            IResolveFieldContext<int?> rfc2 = null;
            Func<IResolveFieldContext<int?>, Task<string>> fn1 = async context =>
            {
                rfc1 = context;
                await Task.Delay(100);
                return "ok";
            };
            var ffr1 = new FuncFieldResolver<int?, object>(context => fn1(context));
            Func<IResolveFieldContext<int?>, Task<string>> fn2 = async context =>
            {
                rfc2 = context;
                await Task.Delay(100);
                return "ok";
            };
            var ffr2 = new FuncFieldResolver<int?, object>(context => fn2(context));
            ffr1.Resolve(_context);
            ffr2.Resolve(_context);
            rfc1.ShouldNotBeNull();
            rfc1.ShouldNotBeSameAs(_context);
            rfc2.ShouldNotBeNull();
            rfc2.ShouldNotBeSameAs(_context);
            rfc1.ShouldNotBe(rfc2);
        }

        [Fact]
        public void Does_Not_Share_Failed_Tasks_Typed_1()
        {
            IResolveFieldContext<int?> rfc1 = null;
            IResolveFieldContext<int?> rfc2 = null;
            var ffr1 = new FuncFieldResolver<int?, Task<string>>(context =>
            {
                rfc1 = context;
                return Task.FromException<string>(new Exception());
            });
            var ffr2 = new FuncFieldResolver<int?, Task<string>>(context =>
            {
                rfc2 = context;
                return Task.FromResult("ok");
            });
            ffr1.Resolve(_context);
            ffr2.Resolve(_context);
            rfc1.ShouldNotBeNull();
            rfc1.ShouldNotBeSameAs(_context);
            rfc2.ShouldNotBeNull();
            rfc2.ShouldNotBeSameAs(_context);
            rfc1.ShouldNotBe(rfc2);
        }

        [Fact]
        public void Does_Not_Share_Failed_Tasks_Typed_2()
        {
            IResolveFieldContext<int?> rfc1 = null;
            IResolveFieldContext<int?> rfc2 = null;
            var ffr1 = new FuncFieldResolver<int?, Task<string>>(context =>
            {
                rfc1 = context;
                throw new Exception();
            });
            var ffr2 = new FuncFieldResolver<int?, Task<string>>(context =>
            {
                rfc2 = context;
                return Task.FromResult("ok");
            });
            try
            {
                ffr1.Resolve(_context);
            }
            catch { }
            ffr2.Resolve(_context);
            rfc1.ShouldNotBeNull();
            rfc1.ShouldNotBeSameAs(_context);
            rfc2.ShouldNotBeNull();
            rfc2.ShouldNotBeSameAs(_context);
            rfc1.ShouldNotBe(rfc2);
        }

        [Fact]
        public void Does_Not_Share_Failed_Tasks_Untyped_1()
        {
            IResolveFieldContext<int?> rfc1 = null;
            IResolveFieldContext<int?> rfc2 = null;
            var ffr1 = new FuncFieldResolver<int?, object>(context =>
            {
                rfc1 = context;
                return Task.FromException<string>(new Exception());
            });
            var ffr2 = new FuncFieldResolver<int?, object>(context =>
            {
                rfc2 = context;
                return Task.FromResult("ok");
            });
            ffr1.Resolve(_context);
            ffr2.Resolve(_context);
            rfc1.ShouldNotBeNull();
            rfc1.ShouldNotBeSameAs(_context);
            rfc2.ShouldNotBeNull();
            rfc2.ShouldNotBeSameAs(_context);
            rfc1.ShouldNotBe(rfc2);
        }

        [Fact]
        public void Does_Not_Share_Failed_Tasks_Untyped_2()
        {
            IResolveFieldContext<int?> rfc1 = null;
            IResolveFieldContext<int?> rfc2 = null;
            var ffr1 = new FuncFieldResolver<int?, object>(context =>
            {
                rfc1 = context;
                throw new Exception();
            });
            var ffr2 = new FuncFieldResolver<int?, object>(context =>
            {
                rfc2 = context;
                return Task.FromResult("ok");
            });
            try
            {
                ffr1.Resolve(_context);
            }
            catch { }
            ffr2.Resolve(_context);
            rfc1.ShouldNotBeNull();
            rfc1.ShouldNotBeSameAs(_context);
            rfc2.ShouldNotBeNull();
            rfc2.ShouldNotBeSameAs(_context);
            rfc1.ShouldNotBe(rfc2);
        }

        [Fact]
        public void Does_Not_Share_Dataloader_Typed()
        {
            IResolveFieldContext<int?> rfc1 = null;
            IResolveFieldContext<int?> rfc2 = null;
            var ffr1 = new FuncFieldResolver<int?, IDataLoaderResult>(context =>
            {
                rfc1 = context;
                return _okDataLoader;
            });
            var ffr2 = new FuncFieldResolver<int?, IDataLoaderResult>(context =>
            {
                rfc2 = context;
                return _okDataLoader;
            });
            ffr1.Resolve(_context);
            ffr2.Resolve(_context);
            rfc1.ShouldNotBeNull();
            rfc1.ShouldNotBeSameAs(_context);
            rfc2.ShouldNotBeNull();
            rfc2.ShouldNotBeSameAs(_context);
            rfc1.ShouldNotBe(rfc2);
        }

        [Fact]
        public void Does_Not_Share_Dataloader_Untyped()
        {
            IResolveFieldContext<int?> rfc1 = null;
            IResolveFieldContext<int?> rfc2 = null;
            var ffr1 = new FuncFieldResolver<int?, object>(context =>
            {
                rfc1 = context;
                return _okDataLoader;
            });
            var ffr2 = new FuncFieldResolver<int?, object>(context =>
            {
                rfc2 = context;
                return _okDataLoader;
            });
            ffr1.Resolve(_context);
            ffr2.Resolve(_context);
            rfc1.ShouldNotBeNull();
            rfc1.ShouldNotBeSameAs(_context);
            rfc2.ShouldNotBeNull();
            rfc2.ShouldNotBeSameAs(_context);
            rfc1.ShouldNotBe(rfc2);
        }

        [Fact]
        public void Does_Not_Share_Dataloader_Tasks_Typed()
        {
            IResolveFieldContext<int?> rfc1 = null;
            IResolveFieldContext<int?> rfc2 = null;
            var ffr1 = new FuncFieldResolver<int?, Task<IDataLoaderResult>>(context =>
            {
                rfc1 = context;
                return Task.FromResult<IDataLoaderResult>(_okDataLoader);
            });
            var ffr2 = new FuncFieldResolver<int?, Task<IDataLoaderResult>>(context =>
            {
                rfc2 = context;
                return Task.FromResult<IDataLoaderResult>(_okDataLoader);
            });
            ffr1.Resolve(_context);
            ffr2.Resolve(_context);
            rfc1.ShouldNotBeNull();
            rfc1.ShouldNotBeSameAs(_context);
            rfc2.ShouldNotBeNull();
            rfc2.ShouldNotBeSameAs(_context);
            rfc1.ShouldNotBe(rfc2);
        }

        [Fact]
        public void Does_Not_Share_Dataloader_Tasks_Untyped()
        {
            IResolveFieldContext<int?> rfc1 = null;
            IResolveFieldContext<int?> rfc2 = null;
            var ffr1 = new FuncFieldResolver<int?, object>(context =>
            {
                rfc1 = context;
                return Task.FromResult<IDataLoaderResult>(_okDataLoader);
            });
            var ffr2 = new FuncFieldResolver<int?, object>(context =>
            {
                rfc2 = context;
                return Task.FromResult<IDataLoaderResult>(_okDataLoader);
            });
            ffr1.Resolve(_context);
            ffr2.Resolve(_context);
            rfc1.ShouldNotBeNull();
            rfc1.ShouldNotBeSameAs(_context);
            rfc2.ShouldNotBeNull();
            rfc2.ShouldNotBeSameAs(_context);
            rfc1.ShouldNotBe(rfc2);
        }

        [Fact]
        public void Does_Not_Share_Dataloader_Tasks_Typed_Derived()
        {
            IResolveFieldContext<int?> rfc1 = null;
            IResolveFieldContext<int?> rfc2 = null;
            var ffr1 = new FuncFieldResolver<int?, Task<OkDataLoader>>(context =>
            {
                rfc1 = context;
                return Task.FromResult(_okDataLoader);
            });
            var ffr2 = new FuncFieldResolver<int?, Task<OkDataLoader>>(context =>
            {
                rfc2 = context;
                return Task.FromResult(_okDataLoader);
            });
            ffr1.Resolve(_context);
            ffr2.Resolve(_context);
            rfc1.ShouldNotBeNull();
            rfc1.ShouldNotBeSameAs(_context);
            rfc2.ShouldNotBeNull();
            rfc2.ShouldNotBeSameAs(_context);
            rfc1.ShouldNotBe(rfc2);
        }

        [Fact]
        public void Does_Not_Share_Dataloader_Tasks_Untyped_Derived()
        {
            IResolveFieldContext<int?> rfc1 = null;
            IResolveFieldContext<int?> rfc2 = null;
            var ffr1 = new FuncFieldResolver<int?, object>(context =>
            {
                rfc1 = context;
                return Task.FromResult(_okDataLoader);
            });
            var ffr2 = new FuncFieldResolver<int?, object>(context =>
            {
                rfc2 = context;
                return Task.FromResult(_okDataLoader);
            });
            ffr1.Resolve(_context);
            ffr2.Resolve(_context);
            rfc1.ShouldNotBeNull();
            rfc1.ShouldNotBeSameAs(_context);
            rfc2.ShouldNotBeNull();
            rfc2.ShouldNotBeSameAs(_context);
            rfc1.ShouldNotBe(rfc2);
        }
        //====

        [Fact]
        public void Does_Not_Share_Enumerable_Typed()
        {
            IResolveFieldContext<int?> rfc1 = null;
            IResolveFieldContext<int?> rfc2 = null;
            var ffr1 = new FuncFieldResolver<int?, IEnumerable<int>>(context =>
            {
                rfc1 = context;
                return new[] { 1, 2 };
            });
            var ffr2 = new FuncFieldResolver<int?, IEnumerable<int>>(context =>
            {
                rfc2 = context;
                return new[] { 1, 2 };
            });
            ffr1.Resolve(_context);
            ffr2.Resolve(_context);
            rfc1.ShouldNotBeNull();
            rfc1.ShouldNotBeSameAs(_context);
            rfc2.ShouldNotBeNull();
            rfc2.ShouldNotBeSameAs(_context);
            rfc1.ShouldNotBe(rfc2);
        }

        [Fact]
        public void Does_Not_Share_Enumerable_Untyped()
        {
            IResolveFieldContext<int?> rfc1 = null;
            IResolveFieldContext<int?> rfc2 = null;
            var ffr1 = new FuncFieldResolver<int?, object>(context =>
            {
                rfc1 = context;
                return new[] { 1, 2 };
            });
            var ffr2 = new FuncFieldResolver<int?, object>(context =>
            {
                rfc2 = context;
                return new[] { 1, 2 };
            });
            ffr1.Resolve(_context);
            ffr2.Resolve(_context);
            rfc1.ShouldNotBeNull();
            rfc1.ShouldNotBeSameAs(_context);
            rfc2.ShouldNotBeNull();
            rfc2.ShouldNotBeSameAs(_context);
            rfc1.ShouldNotBe(rfc2);
        }

        [Fact]
        public void Does_Not_Share_Enumerable_Tasks_Typed()
        {
            IResolveFieldContext<int?> rfc1 = null;
            IResolveFieldContext<int?> rfc2 = null;
            var ffr1 = new FuncFieldResolver<int?, Task<IEnumerable<int>>>(context =>
            {
                rfc1 = context;
                return Task.FromResult<IEnumerable<int>>(new[] { 1, 2 });
            });
            var ffr2 = new FuncFieldResolver<int?, Task<IEnumerable<int>>>(context =>
            {
                rfc2 = context;
                return Task.FromResult<IEnumerable<int>>(new[] { 1, 2 });
            });
            ffr1.Resolve(_context);
            ffr2.Resolve(_context);
            rfc1.ShouldNotBeNull();
            rfc1.ShouldNotBeSameAs(_context);
            rfc2.ShouldNotBeNull();
            rfc2.ShouldNotBeSameAs(_context);
            rfc1.ShouldNotBe(rfc2);
        }

        [Fact]
        public void Does_Not_Share_Enumerable_Tasks_Untyped()
        {
            IResolveFieldContext<int?> rfc1 = null;
            IResolveFieldContext<int?> rfc2 = null;
            var ffr1 = new FuncFieldResolver<int?, object>(context =>
            {
                rfc1 = context;
                return Task.FromResult<IEnumerable<int>>(new[] { 1, 2 });
            });
            var ffr2 = new FuncFieldResolver<int?, object>(context =>
            {
                rfc2 = context;
                return Task.FromResult<IEnumerable<int>>(new[] { 1, 2 });
            });
            ffr1.Resolve(_context);
            ffr2.Resolve(_context);
            rfc1.ShouldNotBeNull();
            rfc1.ShouldNotBeSameAs(_context);
            rfc2.ShouldNotBeNull();
            rfc2.ShouldNotBeSameAs(_context);
            rfc1.ShouldNotBe(rfc2);
        }

    }
}
