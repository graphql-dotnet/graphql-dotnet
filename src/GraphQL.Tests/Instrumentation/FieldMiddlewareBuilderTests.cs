using GraphQL.Instrumentation;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Instrumentation;

public class FieldMiddlewareBuilderTests
{
    private readonly FieldMiddlewareBuilder _builder;
    private readonly ResolveFieldContext _context;

    public FieldMiddlewareBuilderTests()
    {
        _builder = new FieldMiddlewareBuilder();
        _context = new ResolveFieldContext
        {
            FieldDefinition = new FieldType { Name = "Name" },
            FieldAst = new GraphQLField { Name = new GraphQLName("Name") },
            Source = new Person { Name = "Quinn" },
            Errors = new ExecutionErrors(),
            Schema = new Schema(),
            Metrics = new Metrics().Start(null)
        };
    }

    [Fact]
    public void no_middleware_build_returns_null()
    {
        _builder.BuildResolve().ShouldBeNull();
    }

    [Fact]
    public async Task default_resolves_named_field()
    {
        _builder.Use(next => next);
        (await _builder.BuildResolve().Invoke(_context).ConfigureAwait(false)).ShouldBe("Quinn");
    }

    [Fact]
    public async Task middleware_can_override()
    {
        _builder.Use(next => context => new ValueTask<object>("One"));

        (await _builder.BuildResolve().Invoke(_context).ConfigureAwait(false)).ShouldBe("One");
    }

    [Fact]
    public async Task multiple_middleware_runs_in_correct_order()
    {
        // verify that the middleware runs in the same order as it did in 3.x

        _builder.Use(next =>
        {
            return async context =>
            {
                var res = await next(context).ConfigureAwait(false);
                return "One " + res;
            };
        });
        _builder.Use(next =>
        {
            return async context =>
            {
                var res = await next(context).ConfigureAwait(false);
                return "Two " + res;
            };
        });

        var result = await _builder.BuildResolve().Invoke(_context).ConfigureAwait(false);
        result.ShouldBe("One Two Quinn");
    }

    [Fact]
    public async Task middleware_can_combine()
    {
        _builder.Use(next =>
        {
            return async context =>
            {
                var res = await next(context).ConfigureAwait(false);
                return "One " + res;
            };
        });

        var result = await _builder.BuildResolve().Invoke(_context).ConfigureAwait(false);
        result.ShouldBe("One Quinn");
    }

    [Fact]
    public async Task middleware_can_compose()
    {
        _builder.Use(next =>
        {
            return context =>
            {
                using (context.Metrics.Subject("test", "testing name"))
                {
                    return next(context);
                }
            };
        });

        var result = await _builder.BuildResolve().Invoke(_context).ConfigureAwait(false);
        result.ShouldBe("Quinn");

        var record = _context.Metrics.Finish().Skip(1).Single();
        record.Category.ShouldBe("test");
        record.Subject.ShouldBe("testing name");
    }

    [Fact]
    public async Task can_use_class()
    {
        _builder.Use(new SimpleMiddleware());

        var result = await _builder.BuildResolve().Invoke(_context).ConfigureAwait(false);
        result.ShouldBe("Quinn");

        var record = _context.Metrics.Finish().Skip(1).Single();
        record.Category.ShouldBe("class");
        record.Subject.ShouldBe("from class");
    }

    [Fact]
    public async Task can_report_errors()
    {
        _builder.Use(next =>
        {
            return context =>
            {
                context.Errors.Add(new ExecutionError("Custom error"));
                return default;
            };
        });

        var result = await _builder.BuildResolve().Invoke(_context).ConfigureAwait(false);
        result.ShouldBeNull();
        _context.Errors.ShouldContain(x => x.Message == "Custom error");
    }

    [Fact]
    public async Task can_report_errors_with_data()
    {
        var additionalData = new Dictionary<string, string[]>
        {
            ["errorCodes"] = new[] { "one", "two" },
            ["otherErrorCodes"] = new[] { "one", "four" }
        };
        _builder.Use(next =>
        {
            return context =>
            {
                context.Errors.Add(new ExecutionError("Custom error", additionalData));
                return default;
            };
        });

        var result = await _builder.BuildResolve().Invoke(_context).ConfigureAwait(false);

        result.ShouldBeNull();
        _context.Errors.ShouldContain(x => x.Message == "Custom error");
        AssertData(_context.Errors.Single(), additionalData);
    }

    private static void AssertData(ExecutionError errors, Dictionary<string, string[]> additionalData)
    {
        foreach (var ad in additionalData)
            errors.Data[ad.Key].ShouldBe(ad.Value);
    }

    public class Person
    {
        public string Name { get; set; }
    }

    public class SimpleMiddleware : IFieldMiddleware
    {
        public ValueTask<object> ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next)
        {
            using (context.Metrics.Subject("class", "from class"))
            {
                return next(context);
            }
        }
    }
}

internal static class TestExtensions
{
    public static FieldMiddlewareDelegate BuildResolve(this FieldMiddlewareBuilder builder)
    {
        var transform = builder.Build();
        return transform != null ? transform(null) : null;
    }
}
