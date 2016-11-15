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
            _context = new ResolveFieldContext();
            _context.FieldName = "Name";
            _context.FieldAst = new Field(null, new NameNode("Name"));
            _context.Source = new Person
            {
                Name = "Quinn"
            };

            _context.Metrics = new Metrics();
        }

        [Fact]
        public void default_resolves_named_field()
        {
            _builder.Build().Invoke(_context).Result.ShouldBe("Quinn");
        }

        [Fact]
        public void middleware_can_override()
        {
            _builder.Use(next =>
            {
                return context => Task.FromResult<object>("One");
            });

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

            var record = _context.Metrics.AllRecords.Single();
            record.Category.ShouldBe("test");
            record.Subject.ShouldBe("testing name");
        }

        [Fact]
        public void can_use_class()
        {
            _builder.Use<SimpleMiddleware>();

            var result = _builder.Build().Invoke(_context).Result;
            result.ShouldBe("Quinn");

            var record = _context.Metrics.AllRecords.Single();
            record.Category.ShouldBe("class");
            record.Subject.ShouldBe("from class");
        }

        public class Person
        {
            public string Name { get; set; }
        }

        public class SimpleMiddleware
        {
            public Task<object> Resolve(ResolveFieldContext context, FieldMiddlewareDelegate next)
            {
                using (context.Metrics.Subject("class", "from class"))
                {
                    return next(context);
                }
            }
        }
    }
}
