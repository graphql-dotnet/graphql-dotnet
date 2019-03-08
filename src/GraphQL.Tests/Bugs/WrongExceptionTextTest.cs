using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public sealed class WrongExceptionTextTest : QueryTestBase<WrongExceptionTextTest.BugSchema>
    {
        public sealed class BugSchema : Schema
        {
            public BugSchema()
            {
                var query = new ObjectGraphType();
                query.Field<BugType>("query", resolve: _ => new TypeB()); // Here's a misuse
                Query = query;
            }
        }

        private sealed class TypeA { }

        private sealed class TypeB { }

        private sealed class BugType : ObjectGraphType<TypeA>
        {
            public BugType() => Field<StringGraphType>("test", resolve: _ => null);
        }

        [Fact]
        public void wrong_return_type_exception_text()
        {
            var expectedResult = new ExecutionErrors {
                new ExecutionError($"Expected value of type \"{typeof(TypeA).FullName}\" for \"{typeof(BugType).Name}\" but got: \"{typeof(TypeB).FullName}\".")
            };

            AssertQueryErrors("{ query { test } }", expectedResult);
        }
    }
}
