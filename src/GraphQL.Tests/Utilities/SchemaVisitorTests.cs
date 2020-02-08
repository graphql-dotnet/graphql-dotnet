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
                field.Resolver = new SetResultFieldResolver(async context =>
                {
                    await inner.SetResultAsync(context);
                    if (context.Result is string str)
                        context.Result = str.ToUpperInvariant();
                });
            }
        }

        public class Query
        {
            public Task<string> Hello()
            {
                return Task.FromResult("Hello World2!");
            }
        }
    }
}
