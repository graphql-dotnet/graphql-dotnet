using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.Resolvers;

namespace GraphQL.Tests.Execution;

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
            OutputExtensions = new Dictionary<string, object>(),
        };
    }

    [Fact]
    public async Task Pass_Through_Object_Source()
    {
        IResolveFieldContext<object> rfc1 = null;
        var ffr1 = new FuncFieldResolver<object, string>(context =>
        {
            rfc1 = context;
            return "ok";
        });
        await ffr1.ResolveAsync(_context).ConfigureAwait(false);
        rfc1.ShouldNotBeNull();
        rfc1.ShouldBeSameAs(_context);
    }

    [Fact]
    public async Task Shares_Complete_Typed()
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
        await ffr1.ResolveAsync(_context).ConfigureAwait(false);
        await ffr2.ResolveAsync(_context).ConfigureAwait(false);
        rfc1.ShouldNotBeNull();
        rfc1.ShouldNotBeSameAs(_context);
        rfc2.ShouldNotBeNull();
        rfc2.ShouldNotBeSameAs(_context);
        rfc1.ShouldBe(rfc2);
    }

    [Fact]
    public async Task Shares_Complete_Untyped()
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
        await ffr1.ResolveAsync(_context).ConfigureAwait(false);
        await ffr2.ResolveAsync(_context).ConfigureAwait(false);
        rfc1.ShouldNotBeNull();
        rfc1.ShouldNotBeSameAs(_context);
        rfc2.ShouldNotBeNull();
        rfc2.ShouldNotBeSameAs(_context);
        rfc1.ShouldBe(rfc2);
    }

    [Fact]
    public async Task Does_Not_Share_Failed_Typed()
    {
        IResolveFieldContext<int?> rfc1 = null;
        IResolveFieldContext<int?> rfc2 = null;
        Func<IResolveFieldContext<int?>, string> func1 = context =>
        {
            rfc1 = context;
            throw new Exception();
        };
        var ffr1 = new FuncFieldResolver<int?, string>(func1);
        var ffr2 = new FuncFieldResolver<int?, string>(context =>
        {
            rfc2 = context;
            return "ok";
        });
        try
        {
            await ffr1.ResolveAsync(_context).ConfigureAwait(false);
        }
        catch { }
        await ffr2.ResolveAsync(_context).ConfigureAwait(false);
        rfc1.ShouldNotBeNull();
        rfc1.ShouldNotBeSameAs(_context);
        rfc2.ShouldNotBeNull();
        rfc2.ShouldNotBeSameAs(_context);
        rfc1.ShouldNotBe(rfc2);
    }

    [Fact]
    public async Task Does_Not_Share_Failed_Untyped()
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
            await ffr1.ResolveAsync(_context).ConfigureAwait(false);
        }
        catch { }
        await ffr2.ResolveAsync(_context).ConfigureAwait(false);
        rfc1.ShouldNotBeNull();
        rfc1.ShouldNotBeSameAs(_context);
        rfc2.ShouldNotBeNull();
        rfc2.ShouldNotBeSameAs(_context);
        rfc1.ShouldNotBe(rfc2);
    }

    [Fact]
    public async Task Does_Not_Share_Dataloader_Typed()
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
        await ffr1.ResolveAsync(_context).ConfigureAwait(false);
        await ffr2.ResolveAsync(_context).ConfigureAwait(false);
        rfc1.ShouldNotBeNull();
        rfc1.ShouldNotBeSameAs(_context);
        rfc2.ShouldNotBeNull();
        rfc2.ShouldNotBeSameAs(_context);
        rfc1.ShouldNotBe(rfc2);
    }

    [Fact]
    public async Task Does_Not_Share_Dataloader_Untyped()
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
        await ffr1.ResolveAsync(_context).ConfigureAwait(false);
        await ffr2.ResolveAsync(_context).ConfigureAwait(false);
        rfc1.ShouldNotBeNull();
        rfc1.ShouldNotBeSameAs(_context);
        rfc2.ShouldNotBeNull();
        rfc2.ShouldNotBeSameAs(_context);
        rfc1.ShouldNotBe(rfc2);
    }

    [Fact]
    public async Task Does_Not_Share_Enumerable_Typed()
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
        await ffr1.ResolveAsync(_context).ConfigureAwait(false);
        await ffr2.ResolveAsync(_context).ConfigureAwait(false);
        rfc1.ShouldNotBeNull();
        rfc1.ShouldNotBeSameAs(_context);
        rfc2.ShouldNotBeNull();
        rfc2.ShouldNotBeSameAs(_context);
        rfc1.ShouldNotBe(rfc2);
    }

    [Fact]
    public async Task Does_Not_Share_Enumerable_Untyped()
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
        await ffr1.ResolveAsync(_context).ConfigureAwait(false);
        await ffr2.ResolveAsync(_context).ConfigureAwait(false);
        rfc1.ShouldNotBeNull();
        rfc1.ShouldNotBeSameAs(_context);
        rfc2.ShouldNotBeNull();
        rfc2.ShouldNotBeSameAs(_context);
        rfc1.ShouldNotBe(rfc2);
    }
}
