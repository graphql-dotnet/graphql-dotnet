using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Utilities;
using Xunit;

namespace GraphQL.Tests.Utilities
{
    public class SchemaVisitorTests : SchemaBuilderTestBase
    {
        [Fact]
        public void can_create_basic_custom_directive()
        {
            Builder.RegisterDirectiveVisitor<UppercaseDirectiveVisitor>("upper");

            AssertQuery(_ =>
            {
                _.Definitions = @"
                    type Query {
                        hello: String @upper
                    }
                ";

                _.Query = "{ hello }";
                _.Root = new { Hello = "Hello World!" };
                _.ExpectedResult = @"{ ""hello"": ""HELLO WORLD!"" }";
            });
        }

        public class UppercaseDirectiveVisitor : SchemaDirectiveVisitor
        {
            public override void VisitFieldDefinition(FieldType field)
            {
                var inner = field.Resolver ?? NameFieldResolver.Instance;
                field.Resolver = new FuncFieldResolver<object>(context =>
                {
                    var result = inner.Resolve(context);

                    if (result is string str)
                    {
                        return str.ToUpperInvariant();
                    }

                    return result;
                });
            }
        }

        [Fact]
        public void can_create_custom_directive_with_tasks()
        {
            Builder.RegisterDirectiveVisitor<AsyncUppercaseDirectiveVisitor>("upper");
            Builder.Types.Include<Query>();

            AssertQuery(_ =>
            {
                _.Definitions = @"
                    type Query {
                        hello: String @upper
                    }
                ";

                _.Query = "{ hello }";
                _.ExpectedResult = @"{ ""hello"": ""HELLO WORLD2!"" }";
            });
        }

        public class Query
        {
            public Task<string> Hello()
            {
                return Task.FromResult("Hello World2!");
            }
        }

        public class AsyncUppercaseDirectiveVisitor : SchemaDirectiveVisitor
        {
            public override void VisitFieldDefinition(FieldType field)
            {
                var inner = WrapResolver(field.Resolver);
                field.Resolver = new AsyncFieldResolver<object>(async context =>
                {
                    var result = await inner.ResolveAsync(context);

                    if (result is string str)
                    {
                        return str.ToUpperInvariant();
                    }

                    return result;
                });
            }
        }
    }
}
