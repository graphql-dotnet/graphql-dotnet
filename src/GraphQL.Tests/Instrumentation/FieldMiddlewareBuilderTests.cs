using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Types;
using Shouldly;
using Xunit;
using Field = GraphQL.Language.AST.Field;

namespace GraphQL.Tests.Instrumentation
{
    public class FieldMiddlewareBuilderTests
    {
        private readonly FieldMiddlewareBuilder _builder;
        private readonly ResolveFieldContext _context;

        public FieldMiddlewareBuilderTests()
        {
            _builder = new FieldMiddlewareBuilder();
            _context = new ResolveFieldContext
            {
                FieldName = "Name",
                FieldAst = new Field(null, new NameNode("Name")),
                Source = new Person { Name = "Quinn" },
                Errors = new ExecutionErrors(),
                Metrics = new Metrics().Start(null)
            };
        }

        [Fact]
        public void default_resolves_named_field()
        {
            _builder.Build().Invoke(_context).Result.ShouldBe("Quinn");
        }

        [Fact]
        public void middleware_can_override()
        {
            _builder.Use(next => context => Task.FromResult<object>("One"));

            _builder.Build().Invoke(_context).Result.ShouldBe("One");
        }

        [Fact]
        public void middleware_can_combine()
        {
            _builder.Use(next =>
            {
                return async context =>
                {
                    var res = await next(context);
                    return "One " + res.ToString();
                };
            });

            var result = _builder.Build().Invoke(_context).Result;
            result.ShouldBe("One Quinn");
        }

        [Fact]
        public void middleware_can_compose()
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

            var result = _builder.Build().Invoke(_context).Result;
            result.ShouldBe("Quinn");

            var record = _context.Metrics.Finish().Skip(1).Single();
            record.Category.ShouldBe("test");
            record.Subject.ShouldBe("testing name");
        }

        [Fact]
        public void can_use_class()
        {
            _builder.Use<SimpleMiddleware>();

            var result = _builder.Build().Invoke(_context).Result;
            result.ShouldBe("Quinn");

            var record = _context.Metrics.Finish().Skip(1).Single();
            record.Category.ShouldBe("class");
            record.Subject.ShouldBe("from class");
        }

        [Fact]
        public void can_report_errors()
        {
            _builder.Use(next =>
            {
                return context =>
                {
                    context.Errors.Add(new ExecutionError("Custom error"));
                    return Task.FromResult((object)null);
                };
            });

            var result = _builder.Build().Invoke(_context).Result;
            result.ShouldBeNull();
            _context.Errors.ShouldContain(x => x.Message == "Custom error");
        }

        [Fact]
        public void can_report_errors_with_data()
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
                    return Task.FromResult((object)null);
                };
            });

            var result = _builder.Build().Invoke(_context).Result;

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

        public class SimpleMiddleware
        {
            public Task<object> Resolve(IResolveFieldContext context, FieldMiddlewareDelegate next)
            {
                using (context.Metrics.Subject("class", "from class"))
                {
                    return next(context);
                }
            }
        }
    }
}
