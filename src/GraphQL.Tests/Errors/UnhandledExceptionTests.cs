using System;
using GraphQL.Tests.Utilities;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Errors
{
    public class UnhandledExceptionTests : SchemaBuilderTestBase
    {
        [Fact]
        public void rethrows_unhandled_exception()
        {
            var def = @"
                type Query {
                  hello: Int
                }
            ";

            Builder.Types.Include<Query>();

            Should.Throw<AggregateException>(() =>
            {
                AssertQuery(_ =>
                {
                    _.Query = "{ hello }";
                    _.Definitions = def;
                    _.ThrowOnUnhandledException = true;
                });
            });
        }

        public class Query
        {
            public int Hello()
            {
                throw new Exception("arrgh");
            }
        }
    }
}
