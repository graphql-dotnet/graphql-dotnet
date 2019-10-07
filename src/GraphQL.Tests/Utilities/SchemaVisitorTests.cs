using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Utilities;
using Shouldly;
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
                _.ExpectedResult = "{ 'hello': 'HELLO WORLD!' }";
            });
        }

        public class UppercaseDirectiveVisitor : SchemaDirectiveVisitor
        {
            public override void VisitField(FieldType field)
            {
                var inner = field.Resolver ?? new NameFieldResolver();
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
                _.ExpectedResult = "{ 'hello': 'HELLO WORLD2!' }";
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
            public override void VisitField(FieldType field)
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

        [Fact]
        public void can_apply_custom_directive_when_graph_type_first()
        {
            var objectType = new ObjectGraphType();
            objectType.Field<StringGraphType>()
                .Name("hello")
                .Resolve(_ => "Hello World!")
                .Directive("upper", new UppercaseDirectiveVisitor());

            var directivesMetadata = objectType.Fields.First().GetDirectives();
            directivesMetadata.ShouldNotBeNull();
            directivesMetadata.Count.ShouldBe(1, "Only 1 directive should be added");
            directivesMetadata.ContainsKey("upper").ShouldBeTrue();

            var queryResult = CreateQueryResult("{ 'hello': 'HELLO WORLD!' }");
            var schema = new Schema { Query = objectType };

            AssertQuery(_ =>
            {
                _.Schema = schema;
                _.Query = "{ hello }";
            }, queryResult);
        }
    }
}
